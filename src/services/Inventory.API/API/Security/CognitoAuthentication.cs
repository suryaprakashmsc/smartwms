using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.API.Security;

public static class CognitoAuthentication
{
    public static IServiceCollection AddCognitoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Cognito:Authority"];
        var clientId = configuration["Cognito:ClientId"];
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var issuer) || issuer.Scheme != "https" || string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Cognito Authority (HTTPS) and ClientId are required.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, ValidateIssuer = true, ValidIssuer = authority,
                // Cognito access tokens use client_id, validated below, instead of aud.
                ValidateAudience = false, ValidateLifetime = true, RequireExpirationTime = true,
                RequireSignedTokens = true, ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var principal = context.Principal;
                    if (principal?.FindFirst("token_use")?.Value != "access" ||
                        principal.FindFirst("client_id")?.Value != clientId ||
                        string.IsNullOrWhiteSpace(principal.FindFirst("sub")?.Value))
                        context.Fail("A valid access token for this application is required.");
                    return Task.CompletedTask;
                }
            };
        });
        services.AddAuthorization(options => options.AddPolicy("InventoryWrite", policy =>
            policy.RequireAuthenticatedUser().RequireClaim("cognito:groups", "inventory-writers")));
        return services;
    }
}
