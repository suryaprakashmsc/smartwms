namespace Inventory.Domain.ValueObjects;

/// <summary>
/// Represents a Stock Keeping Unit identifier.
/// Format enforced: CATEGORY-ALPHANUMERIC (e.g. ELEC-001, PKG-A12X)
/// Immutable value object.
/// </summary>
public sealed class SKU : IEquatable<SKU>
{
    public string Value { get; } = string.Empty;

    // Allowed format: 2-6 uppercase letters, hyphen, 2-10 uppercase alphanumeric
    private static readonly System.Text.RegularExpressions.Regex _pattern =
        new(@"^[A-Z]{2,6}-[A-Z0-9]{2,10}$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private SKU() { } // EF Core

    private SKU(string value) => Value = value;

    // -- Factory methods ----------------------------------------------------

    public static SKU Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU cannot be empty.", nameof(value));

        var normalized = value.Trim().ToUpperInvariant();

        if (!_pattern.IsMatch(normalized))
            throw new ArgumentException(
                $"Invalid SKU format: '{value}'. " +
                "Expected format: CATEGORY-ALPHANUMERIC (e.g. ELEC-001, PKG-A12X).",
                nameof(value));

        return new SKU(normalized);
    }

    /// <summary>
    /// Creates a SKU from category prefix and item code.
    /// E.g. Generate("ELEC", "001") ? ELEC-001
    /// </summary>
    public static SKU Generate(string categoryPrefix, string itemCode)
    {
        if (string.IsNullOrWhiteSpace(categoryPrefix))
            throw new ArgumentException("Category prefix is required.", nameof(categoryPrefix));
        if (string.IsNullOrWhiteSpace(itemCode))
            throw new ArgumentException("Item code is required.", nameof(itemCode));

        return Of($"{categoryPrefix.Trim().ToUpperInvariant()}-{itemCode.Trim().ToUpperInvariant()}");
    }

    // -- Helpers ------------------------------------------------------------

    /// <summary>Returns the category prefix portion e.g. "ELEC" from "ELEC-001"</summary>
    public string CategoryPrefix => Value.Split('-')[0];

    /// <summary>Returns the item code portion e.g. "001" from "ELEC-001"</summary>
    public string ItemCode => Value.Split('-')[1];

    // -- Equality -----------------------------------------------------------

    public bool Equals(SKU? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as SKU);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(SKU? left, SKU? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(SKU? left, SKU? right) => !(left == right);

    // -- Display ------------------------------------------------------------

    public override string ToString() => Value;

    // -- Implicit conversion for convenience -------------------------------

    public static implicit operator string(SKU sku) => sku.Value;
}


