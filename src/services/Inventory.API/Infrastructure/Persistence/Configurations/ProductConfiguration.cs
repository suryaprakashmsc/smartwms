using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasColumnName("category")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Supplier)
            .HasColumnName("supplier")
            .HasMaxLength(200);

        builder.Property(p => p.WarehouseLocation)
            .HasColumnName("warehouse_location")
            .HasMaxLength(100);

        builder.Property(p => p.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(100);

        builder.Property(p => p.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(1000);

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(100);

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.TotalValue);
        builder.Ignore(p => p.IsLowStock);
        builder.Ignore(p => p.IsOutOfStock);
        builder.Ignore(p => p.StockStatus);

        builder.OwnsOne(p => p.SKU, sku =>
        {
            sku.Property(s => s.Value)
                .HasColumnName("sku")
                .HasMaxLength(20)
                .IsRequired();

            sku.HasIndex(s => s.Value)
                .IsUnique();
        });

        builder.OwnsOne(p => p.UnitPrice, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("unit_price")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(p => p.Stock, stock =>
        {
            stock.Property(s => s.Quantity)
                .HasColumnName("stock_quantity")
                .IsRequired();

            stock.Property(s => s.ReorderLevel)
                .HasColumnName("reorder_level")
                .IsRequired();

            stock.Property(s => s.MaximumLevel)
                .HasColumnName("maximum_level");
        });
    }
}
