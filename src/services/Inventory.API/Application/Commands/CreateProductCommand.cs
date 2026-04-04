namespace Inventory.Application.Commands;

public sealed record CreateProductCommand(string Sku, string Name, int QuantityOnHand, Guid CategoryId);
