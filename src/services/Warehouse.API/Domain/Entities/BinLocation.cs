namespace Warehouse.Domain.Entities;

public sealed class BinLocation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
}
