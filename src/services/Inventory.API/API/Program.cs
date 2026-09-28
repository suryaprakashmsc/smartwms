using Inventory.API.Health;
using Inventory.API.Middleware;
using Inventory.API.Security;
using Inventory.Application;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

// Compose needs an executable probe because the runtime has no shell or curl.
// Kubernetes continues to use its native HTTP probes.
if (args.Length == 1 && args[0] == "--healthcheck")
{
    using var handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false };
    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };
    Environment.ExitCode = await ContainerReadinessProbe.RunAsync(client);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerGen();
builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddCognitoAuthentication(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<DatabaseReadiness>("database", tags: new[] { "ready" });
if (builder.Environment.IsDevelopment())
    builder.Services.AddCors(options => options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();

// Explicit one-shot operation, run once before starting API replicas.
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    if (args.Contains("--seed-demo") && !app.Environment.IsEnvironment("Local"))
        throw new InvalidOperationException("Demo seeding is permitted only in the Local environment.");
    await database.Database.MigrateAsync();
    if (args.Contains("--seed-demo"))
        await InventoryDbSeeder.SeedAsync(database, scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
    return;
}
if (args.Contains("--seed-demo"))
    throw new InvalidOperationException("Demo seeding requires the one-shot --migrate operation.");
app.UseExceptionHandler();
app.UseMiddleware<ApiExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("FrontendDev");
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/api/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { service = "inventory-api" }));
app.Run();
public partial class Program { }
