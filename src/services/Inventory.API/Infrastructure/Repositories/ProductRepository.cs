using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.ValueObjects;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _ctx;

    public ProductRepository(InventoryDbContext ctx)
    {
        _ctx = ctx;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySkuAsync(SKU sku, CancellationToken ct = default) =>
        _ctx.Products.FirstOrDefaultAsync(p => p.SKU.Value == sku.Value, ct);

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? category = null,
        bool? lowStockOnly = null,
        bool? activeOnly = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Min(page, int.MaxValue / pageSize);

        IQueryable<Product> query = _ctx.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (lowStockOnly == true)
        {
            query = query.Where(p => p.Stock.Quantity <= p.Stock.ReorderLevel);
        }

        if (activeOnly.HasValue)
        {
            query = query.Where(p => p.IsActive == activeOnly.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = $"%{searchTerm.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, term) ||
                EF.Functions.ILike(p.SKU.Value, term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync(CancellationToken ct = default) =>
        await _ctx.Products
            .AsNoTracking()
            .Where(p => p.Stock.Quantity <= p.Stock.ReorderLevel)
            .ToListAsync(ct);

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        await _ctx.Products
            .AsNoTracking()
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);

    public Task<bool> SkuExistsAsync(SKU sku, Guid? excludeProductId = null, CancellationToken ct = default)
    {
        var query = _ctx.Products.Where(p => p.SKU.Value == sku.Value);

        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return query.AnyAsync(ct);
    }

    public Task AddAsync(Product product, CancellationToken ct = default) =>
        _ctx.Products.AddAsync(product, ct).AsTask();

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _ctx.Products.Update(product);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(id, ct);
        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{id}' was not found.");
        }

        product.Discontinue("system");
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _ctx.SaveChangesAsync(ct);
}
