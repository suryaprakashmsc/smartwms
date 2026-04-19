using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.ValueObjects;

namespace Inventory.Infrastructure.Repositories;

public sealed class InMemoryInventoryRepository : IProductRepository
{
    private static readonly object SyncRoot = new();
    private static readonly List<Product> Products = BuildSeedProducts();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            return Task.FromResult(Products.SingleOrDefault(p => p.Id == id));
        }
    }

    public Task<Product?> GetBySkuAsync(SKU sku, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            return Task.FromResult(Products.SingleOrDefault(p => p.SKU == sku));
        }
    }

    public Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? category = null,
        bool? lowStockOnly = null,
        bool? activeOnly = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;

        lock (SyncRoot)
        {
            IEnumerable<Product> query = Products;

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (lowStockOnly == true)
            {
                query = query.Where(p => p.IsLowStock);
            }

            if (activeOnly.HasValue)
            {
                query = query.Where(p => p.IsActive == activeOnly.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p =>
                    p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.SKU.Value.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var total = query.Count();
            var items = query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult<(IEnumerable<Product> Items, int TotalCount)>((items, total));
        }
    }

    public Task<IEnumerable<Product>> GetLowStockAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            return Task.FromResult<IEnumerable<Product>>(Products.Where(p => p.IsLowStock).ToArray());
        }
    }

    public Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            return Task.FromResult<IEnumerable<string>>(Products
                .Select(p => p.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToArray());
        }
    }

    public Task<bool> SkuExistsAsync(SKU sku, Guid? excludeProductId = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var exists = Products.Any(p =>
                p.SKU == sku &&
                (!excludeProductId.HasValue || p.Id != excludeProductId.Value));

            return Task.FromResult(exists);
        }
    }

    public Task AddAsync(Product product, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            Products.Add(product);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var index = Products.FindIndex(p => p.Id == product.Id);
            if (index < 0)
            {
                throw new KeyNotFoundException($"Product '{product.Id}' was not found.");
            }

            Products[index] = product;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var product = Products.SingleOrDefault(p => p.Id == id);
            if (product is null)
            {
                throw new KeyNotFoundException($"Product '{id}' was not found.");
            }

            product.Discontinue("system");
        }

        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(1);
    }

    private static List<Product> BuildSeedProducts()
    {
        var scanner = Product.Create(
            "Handheld Scanner",
            "WMS-001",
            "Hardware",
            275.00m,
            10,
            "system",
            "Industrial handheld barcode scanner",
            "Acme Supplies",
            "A1-R1",
            currency: "USD",
            maximumLevel: 250);

        scanner.AdjustStock(24, "Initial seed", "system");

        var labels = Product.Create(
            "Thermal Label Roll",
            "WMS-002",
            "Consumables",
            3.25m,
            50,
            "system",
            "4x6 thermal labels",
            "LabelWorld",
            "B2-R3",
            currency: "USD",
            maximumLevel: 5000);

        labels.AdjustStock(180, "Initial seed", "system");

        return [scanner, labels];
    }
}
