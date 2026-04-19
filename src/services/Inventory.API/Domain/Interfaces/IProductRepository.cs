using Inventory.Domain.Entities;
using Inventory.Domain.ValueObjects;

namespace Inventory.Domain.Interfaces;

/// <summary>
/// Repository contract for Product persistence.
/// Lives in the Domain layer — no EF Core or infrastructure dependencies here.
/// The Infrastructure layer provides the concrete implementation.
/// </summary>
public interface IProductRepository
{
    // -- Queries ------------------------------------------------------------

    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Product?> GetBySkuAsync(SKU sku, CancellationToken ct = default);

    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? category = null,
        bool? lowStockOnly = null,
        bool? activeOnly = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<IEnumerable<Product>> GetLowStockAsync(CancellationToken ct = default);

    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default);

    Task<bool> SkuExistsAsync(SKU sku, Guid? excludeProductId = null,
        CancellationToken ct = default);

    // -- Commands -----------------------------------------------------------

    Task AddAsync(Product product, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>
    /// Soft delete — sets IsActive = false.
    /// Products are never hard-deleted to preserve audit trails.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    // -- Unit of work -------------------------------------------------------

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}


