using Warehouse.Application;
using Warehouse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddWarehouseApplication();
builder.Services.AddWarehouseInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "warehouse-api",
    status = "running",
    capability = "bin-location-management"
}));

app.Run();
