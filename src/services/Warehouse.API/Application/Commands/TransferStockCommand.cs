namespace Warehouse.Application.Commands;

public sealed record TransferStockCommand(string FromBin, string ToBin, string Sku, int Quantity);
