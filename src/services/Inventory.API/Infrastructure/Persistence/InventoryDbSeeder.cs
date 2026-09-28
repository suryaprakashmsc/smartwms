using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Persistence;

public static class InventoryDbSeeder
{
    public static async Task SeedAsync(InventoryDbContext ctx, ILogger logger)
    {
        if (await ctx.Products.AnyAsync())
        {
            logger.LogInformation("Database already seeded - skipping.");
            return;
        }

        logger.LogInformation("Seeding inventory database...");

        var products = new List<Product>
        {
            Product.Create("Samsung 55\" 4K Smart TV", "ELEC-001", "Electronics", 45000m, 5, "seeder", supplier: "Samsung", warehouseLocation: "Warehouse-A1", barcode: "SAMS-TV-55", currency: "INR", maximumLevel: 10),
            Product.Create("Dell Laptop 15 inch i5", "ELEC-002", "Electronics", 62000m, 3, "seeder", supplier: "Dell", warehouseLocation: "Warehouse-A2", barcode: "DELL-LAP-15", currency: "INR", maximumLevel: 8),
            Product.Create("HP LaserJet Printer", "ELEC-003", "Electronics", 18500m, 5, "seeder", supplier: "HP", warehouseLocation: "Warehouse-A3", barcode: "HP-PRN-LJ", currency: "INR", maximumLevel: 15),
            Product.Create("Logitech Wireless Mouse", "ELEC-004", "Electronics", 1299m, 10, "seeder", supplier: "Logitech", warehouseLocation: "Warehouse-A4", barcode: "LOG-MOU-WL", currency: "INR", maximumLevel: 50),
            Product.Create("Mechanical Keyboard RGB", "ELEC-005", "Electronics", 3499m, 8, "seeder", supplier: "Cosmic Byte", warehouseLocation: "Warehouse-A5", barcode: "CB-KB-RGB", currency: "INR", maximumLevel: 25),

            Product.Create("Bubble Wrap Roll 50m", "PKG-001", "Packaging", 850m, 20, "seeder", supplier: "PackSafe", warehouseLocation: "Warehouse-B1", barcode: "PS-BW-50M", currency: "INR", maximumLevel: 100),
            Product.Create("Corrugated Box 12x12x12", "PKG-002", "Packaging", 45m, 100, "seeder", supplier: "BoxKing", warehouseLocation: "Warehouse-B2", barcode: "BK-CB-12", currency: "INR", maximumLevel: 500),
            Product.Create("Stretch Film 500m", "PKG-003", "Packaging", 1200m, 15, "seeder", supplier: "WrapMaster", warehouseLocation: "Warehouse-B3", barcode: "WM-SF-500", currency: "INR", maximumLevel: 80),
            Product.Create("Packing Tape 48mm", "PKG-004", "Packaging", 120m, 30, "seeder", supplier: "Cello", warehouseLocation: "Warehouse-B4", barcode: "CE-PT-48", currency: "INR", maximumLevel: 200),

            Product.Create("Bosch Drill Machine 13mm", "TOOL-001", "Tools", 4800m, 5, "seeder", supplier: "Bosch", warehouseLocation: "Warehouse-C1", barcode: "BSH-DRL-13", currency: "INR", maximumLevel: 12),
            Product.Create("Stanley Hammer 500g", "TOOL-002", "Tools", 650m, 10, "seeder", supplier: "Stanley", warehouseLocation: "Warehouse-C2", barcode: "STN-HMR-500", currency: "INR", maximumLevel: 30),
            Product.Create("Digital Vernier Caliper", "TOOL-003", "Tools", 899m, 5, "seeder", supplier: "Mitutoyo", warehouseLocation: "Warehouse-C3", barcode: "MIT-VNC-DG", currency: "INR", maximumLevel: 20),
            Product.Create("Heavy Duty Wrench Set", "TOOL-004", "Tools", 2200m, 3, "seeder", supplier: "Taparia", warehouseLocation: "Warehouse-C4", barcode: "TAP-WR-HD", currency: "INR", maximumLevel: 10),

            Product.Create("Stainless Steel Sheet 2mm", "RM-001", "Raw Materials", 3200m, 10, "seeder", supplier: "SAIL", warehouseLocation: "Warehouse-D1", barcode: "SAIL-SS-2MM", currency: "INR", maximumLevel: 25),
            Product.Create("Aluminium Rod 25mm", "RM-002", "Raw Materials", 1800m, 8, "seeder", supplier: "Hindalco", warehouseLocation: "Warehouse-D2", barcode: "HIN-AR-25", currency: "INR", maximumLevel: 20),
            Product.Create("Industrial Grade PVC Pipe", "RM-003", "Raw Materials", 560m, 15, "seeder", supplier: "Astral", warehouseLocation: "Warehouse-D3", barcode: "AST-PVC-IG", currency: "INR", maximumLevel: 60),
            Product.Create("Copper Wire 2.5sqmm 90m", "RM-004", "Raw Materials", 2400m, 10, "seeder", supplier: "Finolex", warehouseLocation: "Warehouse-D4", barcode: "FIN-CW-25", currency: "INR", maximumLevel: 30),

            Product.Create("Office Chair Ergonomic", "FG-001", "Finished Goods", 8500m, 3, "seeder", supplier: "Featherlite", warehouseLocation: "Warehouse-E1", barcode: "FL-CH-ERG", currency: "INR", maximumLevel: 8),
            Product.Create("Steel Almirah 3 Door", "FG-002", "Finished Goods", 12000m, 2, "seeder", supplier: "Godrej", warehouseLocation: "Warehouse-E2", barcode: "GDJ-ALM-3D", currency: "INR", maximumLevel: 5),
            Product.Create("Industrial Work Bench", "FG-003", "Finished Goods", 18000m, 2, "seeder", supplier: "Homag", warehouseLocation: "Warehouse-E3", barcode: "HMG-WB-IND", currency: "INR", maximumLevel: 4)
        };

        var stockMap = new Dictionary<string, int>
        {
            ["ELEC-001"] = 10,
            ["ELEC-002"] = 8,
            ["ELEC-003"] = 15,
            ["ELEC-004"] = 45,
            ["ELEC-005"] = 22,
            ["PKG-001"] = 3,
            ["PKG-002"] = 480,
            ["PKG-003"] = 75,
            ["PKG-004"] = 190,
            ["TOOL-001"] = 9,
            ["TOOL-002"] = 28,
            ["TOOL-003"] = 2,
            ["TOOL-004"] = 8,
            ["RM-001"] = 20,
            ["RM-002"] = 15,
            ["RM-003"] = 55,
            ["RM-004"] = 25,
            ["FG-001"] = 6,
            ["FG-002"] = 3,
            ["FG-003"] = 2
        };

        foreach (var product in products)
        {
            if (stockMap.TryGetValue(product.SKU.Value, out var qty) && qty > 0)
            {
                product.AdjustStock(qty, "Initial stock load", "seeder");
            }

            product.ClearDomainEvents();
        }

        await ctx.Products.AddRangeAsync(products);
        await ctx.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} products successfully.", products.Count);
    }
}
