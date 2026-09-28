using Inventory.Domain.Entities;
using Inventory.Domain.ValueObjects;
using Xunit;

namespace Inventory.Tests;

public class DomainTests
{
    [Fact]
    public void FactoryPreservesMaximumAndRejectsOverstock()
    {
        var product = Product.Create("Test", "TEST-001", "Tools", 10, 2, "test", maximumLevel: 5);
        Assert.Equal(5, product.Stock.MaximumLevel);
        Assert.Throws<InvalidOperationException>(() => product.AdjustStock(6, "receipt", "test"));
        Assert.Equal(0, product.Stock.Quantity);
    }

    [Fact]
    public void StockCannotUnderflowOrOverflow()
    {
        Assert.Throws<InvalidOperationException>(() => StockLevel.Of(1, 0).Adjust(-2));
        Assert.Throws<InvalidOperationException>(() => StockLevel.Of(int.MaxValue, 0).Adjust(1));
    }

    [Fact]
    public void DiscontinuedProductCannotBeAdjusted()
    {
        var product = Product.Create("Test", "TEST-001", "Tools", 10, 2, "test");
        product.Discontinue("test");
        Assert.Throws<InvalidOperationException>(() => product.AdjustStock(1, "receipt", "test"));
    }
}
