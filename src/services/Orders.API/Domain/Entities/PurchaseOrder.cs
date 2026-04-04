namespace Orders.Domain.Entities;

public sealed class PurchaseOrder
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
