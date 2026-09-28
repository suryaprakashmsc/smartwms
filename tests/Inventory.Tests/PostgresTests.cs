using Inventory.Infrastructure.Persistence;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Inventory.Tests;

// CI provides an isolated, disposable PostgreSQL database. Never use the application database.
[Trait("Category", "Postgres")]
public class PostgresTests
{
    [Fact]
    public async Task ConcurrentStockUpdateFailsInsteadOfLosingCommittedChanges()
    {
        var connection = Environment.GetEnvironmentVariable("SMARTWMS_TEST_DATABASE")
            ?? throw new InvalidOperationException("Set SMARTWMS_TEST_DATABASE to a disposable PostgreSQL database.");
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connection).Options;
        await using var first = new InventoryDbContext(options);
        await first.Database.MigrateAsync();
        var product = Product.Create("Concurrent", "TEST-" + Guid.NewGuid().ToString("N")[..8], "Tools", 10, 1, "test", maximumLevel: 20);
        product.AdjustStock(5, "initial", "test");
        first.Products.Add(product);
        await first.SaveChangesAsync();
        await using var second = new InventoryDbContext(options);
        var stale = await second.Products.SingleAsync(p => p.Id == product.Id);
        product.AdjustStock(2, "receipt", "test");
        await first.SaveChangesAsync();
        stale.AdjustStock(3, "receipt", "test");
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var verifier = new InventoryDbContext(options);
        Assert.Equal(7, (await verifier.Products.SingleAsync(p => p.Id == product.Id)).Stock.Quantity);
        await verifier.Products.Where(p => p.Id == product.Id).ExecuteDeleteAsync();
    }
}
