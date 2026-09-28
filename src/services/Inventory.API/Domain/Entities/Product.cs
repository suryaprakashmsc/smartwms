using Inventory.Domain.Events;
using Inventory.Domain.ValueObjects;

namespace Inventory.Domain.Entities;

/// <summary>
/// Product aggregate root.
///
/// Design decisions:
/// - Private setters enforce all state changes go through domain methods
/// - Factory method (Create) ensures the entity is never in an invalid state
/// - Domain events are collected here and dispatched after DB commit
/// - Value objects (Money, SKU, StockLevel) encapsulate their own rules
/// </summary>
public class Product
{
    // -- Identity -----------------------------------------------------------

    public Guid Id { get; private set; }

    // -- Value objects ------------------------------------------------------

    /// <summary>Stock Keeping Unit — unique identifier for this product.</summary>
    public SKU SKU { get; private set; } = null!;

    /// <summary>Unit price with currency.</summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>Current stock quantity, reorder level, and max level.</summary>
    public StockLevel Stock { get; private set; } = null!;

    // -- Primitive properties -----------------------------------------------

    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = null!;
    public string? Supplier { get; private set; }
    public string? WarehouseLocation { get; private set; }
    public string? Barcode { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }

    // -- Audit fields -------------------------------------------------------

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public string? UpdatedBy { get; private set; }

    // -- Domain events ------------------------------------------------------

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Collected domain events — read by the infrastructure layer
    /// and dispatched via MediatR after SaveChanges.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    // -- EF Core constructor (private — never use directly) -----------------

    private Product() { }

    // -- Factory method -----------------------------------------------------

    /// <summary>
    /// Creates a new product in a valid initial state.
    /// This is the ONLY way to create a Product — constructors are private.
    /// </summary>
    public static Product Create(
        string name,
        string skuValue,
        string category,
        decimal unitPrice,
        int reorderLevel,
        string createdBy,
        string? description = null,
        string? supplier = null,
        string? warehouseLocation = null,
        string? barcode = null,
        string currency = "INR",
        int? maximumLevel = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Product name must not exceed 200 characters.", nameof(name));
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy is required.", nameof(createdBy));

        // Build value objects — their own constructors enforce their rules
        var sku = SKU.Of(skuValue);
        var price = Money.Of(unitPrice, currency);
        var stock = StockLevel.Of(0, reorderLevel, maximumLevel);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            SKU = sku,
            UnitPrice = price,
            Stock = stock,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Category = category.Trim(),
            Supplier = supplier?.Trim(),
            WarehouseLocation = warehouseLocation?.Trim(),
            Barcode = barcode?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        // Raise domain event
        product._domainEvents.Add(new ProductCreatedEvent(
            product.Id,
            product.Name,
            product.SKU.Value,
            product.Category,
            product.CreatedAt));

        return product;
    }

    // -- Domain behaviours --------------------------------------------------

    /// <summary>
    /// Adjusts stock up (receiving) or down (dispatch/write-off).
    /// Raises StockAdjustedEvent and LowStockDetectedEvent if threshold crossed.
    /// </summary>
    public void AdjustStock(int delta, string reason, string adjustedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required for stock adjustment.", nameof(reason));
        if (!IsActive)
            throw new InvalidOperationException($"Cannot adjust stock for discontinued product '{Name}'.");

        var previousQty = Stock.Quantity;

        // StockLevel.Adjust enforces no-negative & max-level rules
        Stock = Stock.Adjust(delta);

        SetAudit(adjustedBy);

        _domainEvents.Add(new StockAdjustedEvent(
            Id,
            SKU.Value,
            previousQty,
            Stock.Quantity,
            delta,
            reason,
            DateTime.UtcNow));

        // Raise low-stock alert if threshold just crossed
        if (Stock.IsLow && previousQty > Stock.ReorderLevel)
        {
            _domainEvents.Add(new LowStockDetectedEvent(
                Id,
                Name,
                SKU.Value,
                Category,
                Stock.Quantity,
                Stock.ReorderLevel,
                DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Updates product details. SKU is intentionally immutable after creation.
    /// </summary>
    public void UpdateDetails(
        string name,
        string description,
        string category,
        string? supplier,
        string? warehouseLocation,
        string? barcode,
        string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (!IsActive)
            throw new InvalidOperationException("Cannot update a discontinued product.");

        Name = name.Trim();
        Description = description.Trim();
        Category = category.Trim();
        Supplier = supplier?.Trim();
        WarehouseLocation = warehouseLocation?.Trim();
        Barcode = barcode?.Trim();

        SetAudit(updatedBy);
    }

    /// <summary>
    /// Updates the unit price. Raises a domain event for audit trail.
    /// </summary>
    public void UpdatePrice(decimal newAmount, string updatedBy, string? currency = null)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot update price for a discontinued product.");

        var oldPrice = UnitPrice;
        UnitPrice = Money.Of(newAmount, currency ?? UnitPrice.Currency);

        _domainEvents.Add(new ProductPriceUpdatedEvent(
            Id,
            SKU.Value,
            oldPrice.Amount,
            UnitPrice.Amount,
            UnitPrice.Currency,
            DateTime.UtcNow));

        SetAudit(updatedBy);
    }

    /// <summary>
    /// Updates reorder level and optional maximum level.
    /// </summary>
    public void UpdateStockThresholds(int reorderLevel, int? maximumLevel, string updatedBy)
    {
        Stock = StockLevel.Of(Stock.Quantity, reorderLevel, maximumLevel);
        SetAudit(updatedBy);
    }

    /// <summary>
    /// Marks this product as discontinued. Cannot be reversed.
    /// </summary>
    public void Discontinue(string updatedBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Product is already discontinued.");

        IsActive = false;
        SetAudit(updatedBy);

        _domainEvents.Add(new ProductDiscontinuedEvent(
            Id,
            SKU.Value,
            DateTime.UtcNow));
    }

    // -- Computed properties (no persistence needed) ------------------------

    /// <summary>Total inventory value = unit price × quantity.</summary>
    public Money TotalValue => UnitPrice.Multiply(Stock.Quantity);

    /// <summary>Convenience passthrough to StockLevel.</summary>
    public bool IsLowStock => Stock.IsLow;

    public bool IsOutOfStock => Stock.IsOutOfStock;

    public StockStatus StockStatus => Stock.Status;

    // -- Private helpers ----------------------------------------------------

    private void SetAudit(string user)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = user;
    }
}

