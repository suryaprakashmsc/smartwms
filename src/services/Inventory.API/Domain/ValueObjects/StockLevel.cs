namespace Inventory.Domain.ValueObjects;

/// <summary>
/// Represents the stock quantity and reorder threshold for a product.
/// Encapsulates all stock-level business rules as a value object.
/// </summary>
public sealed class StockLevel : IEquatable<StockLevel>
{
    public int Quantity { get; }
    public int ReorderLevel { get; }
    public int? MaximumLevel { get; }

    private StockLevel() { } // EF Core

    private StockLevel(int quantity, int reorderLevel, int? maximumLevel)
    {
        Quantity = quantity;
        ReorderLevel = reorderLevel;
        MaximumLevel = maximumLevel;
    }

    // -- Factory methods ----------------------------------------------------

    public static StockLevel Of(int quantity, int reorderLevel, int? maximumLevel = null)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(quantity));

        if (reorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative.", nameof(reorderLevel));

        if (maximumLevel.HasValue && maximumLevel.Value < reorderLevel)
            throw new ArgumentException(
                "Maximum level must be greater than reorder level.", nameof(maximumLevel));

        if (maximumLevel.HasValue && quantity > maximumLevel.Value)
            throw new ArgumentException(
                $"Quantity {quantity} exceeds maximum level {maximumLevel}.", nameof(quantity));

        return new StockLevel(quantity, reorderLevel, maximumLevel);
    }

    public static StockLevel Empty(int reorderLevel = 0) =>
        Of(0, reorderLevel);

    // -- Business rules -----------------------------------------------------

    /// <summary>Stock is at or below the reorder threshold.</summary>
    public bool IsLow => Quantity <= ReorderLevel;

    /// <summary>Stock is completely depleted.</summary>
    public bool IsOutOfStock => Quantity == 0;

    /// <summary>Stock is at or above the maximum level (overstocked).</summary>
    public bool IsOverstocked => MaximumLevel.HasValue && Quantity >= MaximumLevel.Value;

    /// <summary>How many units needed to reach reorder level.</summary>
    public int UnitsBeforeReorder => Math.Max(0, Quantity - ReorderLevel);

    /// <summary>Returns new StockLevel after adding/removing units.</summary>
    public StockLevel Adjust(int delta)
    {
        var candidate = (long)Quantity + delta;
        if (candidate > int.MaxValue)
            throw new InvalidOperationException("Adjustment exceeds the supported stock quantity.");
        var newQty = (int)candidate;

        if (newQty < 0)
            throw new InvalidOperationException(
                $"Adjustment of {delta} would result in negative stock ({newQty}). " +
                $"Current stock: {Quantity}.");

        if (MaximumLevel.HasValue && newQty > MaximumLevel.Value)
            throw new InvalidOperationException(
                $"Adjustment of {delta} would exceed maximum level {MaximumLevel}. " +
                $"Current stock: {Quantity}.");

        return new StockLevel(newQty, ReorderLevel, MaximumLevel);
    }

    /// <summary>Returns a new StockLevel with updated reorder threshold.</summary>
    public StockLevel WithReorderLevel(int newReorderLevel) =>
        Of(Quantity, newReorderLevel, MaximumLevel);

    // -- Status label for UI / reporting -----------------------------------

    public StockStatus Status => this switch
    {
        { IsOutOfStock: true } => StockStatus.OutOfStock,
        { IsLow: true } => StockStatus.Low,
        { IsOverstocked: true } => StockStatus.Overstocked,
        _ => StockStatus.Normal
    };

    // -- Equality -----------------------------------------------------------

    public bool Equals(StockLevel? other)
    {
        if (other is null) return false;
        return Quantity == other.Quantity
            && ReorderLevel == other.ReorderLevel
            && MaximumLevel == other.MaximumLevel;
    }

    public override bool Equals(object? obj) => Equals(obj as StockLevel);

    public override int GetHashCode() =>
        HashCode.Combine(Quantity, ReorderLevel, MaximumLevel);

    public static bool operator ==(StockLevel? left, StockLevel? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(StockLevel? left, StockLevel? right) =>
        !(left == right);

    public override string ToString() =>
        MaximumLevel.HasValue
            ? $"{Quantity} units (reorder: {ReorderLevel}, max: {MaximumLevel})"
            : $"{Quantity} units (reorder: {ReorderLevel})";
}

public enum StockStatus
{
    OutOfStock,
    Low,
    Normal,
    Overstocked
}


