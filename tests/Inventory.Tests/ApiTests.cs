using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.ValueObjects;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Inventory.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://cognito-idp.us-east-1.amazonaws.com/test-pool";
    public const string ClientId = "test-client";
    public static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "test-key" };
    public Mock<IProductRepository> Repository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting("Cognito:Authority", Issuer);
        builder.UseSetting("Cognito:ClientId", ClientId);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=127.0.0.1;Port=1;Database=missing;Username=test;Password=test;Timeout=1");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IProductRepository>();
            Repository.Setup(r => r.GetPagedAsync(It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Array.Empty<Product>().AsEnumerable(), 0));
            Repository.Setup(r => r.SkuExistsAsync(It.IsAny<SKU>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            Repository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            services.AddSingleton(Repository.Object);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var metadata = new OpenIdConnectConfiguration { Issuer = Issuer };
                metadata.SigningKeys.Add(Key);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
    }

    public string Token(string tokenUse = "access", string clientId = ClientId, bool expired = false,
        bool writer = false, string issuer = Issuer, bool wrongSignature = false)
    {
        var claims = new List<Claim> { new("sub", "authenticated-user"), new("token_use", tokenUse), new("client_id", clientId) };
        if (writer) claims.Add(new Claim("cognito:groups", "inventory-writers"));
        var key = wrongSignature ? new RsaSecurityKey(RSA.Create(2048)) { KeyId = "wrong" } : Key;
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, ClientId, claims,
            DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(expired ? -5 : 5), new SigningCredentials(key, SecurityAlgorithms.RsaSha256)));
    }
}

public class ApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    public void Dispose() => factory.Dispose();
    private HttpClient Client(string? token = null)
    {
        var client = factory.CreateClient();
        if (token != null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task MissingTokenIsRejected() => Assert.Equal(HttpStatusCode.Unauthorized, (await Client().GetAsync("/api/products")).StatusCode);

    [Theory]
    [InlineData("id", ApiFactory.ClientId, false)]
    [InlineData("access", "another-client", false)]
    [InlineData("access", ApiFactory.ClientId, true)]
    public async Task InvalidTokenClaimsAreRejected(string use, string client, bool expired) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client(factory.Token(use, client, expired)).GetAsync("/api/products")).StatusCode);

    [Fact]
    public async Task WrongSignatureIsRejected() => Assert.Equal(HttpStatusCode.Unauthorized,
        (await Client(factory.Token(wrongSignature: true)).GetAsync("/api/products")).StatusCode);

    [Fact]
    public async Task WrongIssuerIsRejected() => Assert.Equal(HttpStatusCode.Unauthorized,
        (await Client(factory.Token(issuer: "https://other.example")).GetAsync("/api/products")).StatusCode);

    [Fact]
    public async Task ValidAccessTokenCanRead() => Assert.Equal(HttpStatusCode.OK,
        (await Client(factory.Token()).GetAsync("/api/products")).StatusCode);

    [Fact]
    public async Task ReaderCannotWrite() => Assert.Equal(HttpStatusCode.Forbidden,
        (await Client(factory.Token()).PostAsJsonAsync("/api/products", new { name = "test" })).StatusCode);

    [Fact]
    public async Task WriterAuditIdentityComesFromVerifiedToken()
    {
        var response = await Client(factory.Token(writer: true)).PostAsJsonAsync("/api/products", new
        {
            name = "Test", sku = "TEST-001", category = "Tools", unitPrice = 10, reorderLevel = 1,
            initialQuantity = 2, createdBy = "forged-identity", currency = "USD", maximumLevel = 5
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        factory.Repository.Verify(r => r.AddAsync(It.Is<Product>(p => p.CreatedBy == "authenticated-user"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task LivenessDoesNotDependOnDatabaseButReadinessDoes()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/health/ready")).StatusCode);
    }
}
