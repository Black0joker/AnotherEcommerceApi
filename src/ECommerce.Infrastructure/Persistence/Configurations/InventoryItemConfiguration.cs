using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(ii => ii.Id);

        builder.Property(ii => ii.AvailableQuantity)
            .IsRequired();

        builder.Property(ii => ii.ReservedQuantity)
            .IsRequired();

        builder.Property(ii => ii.RowVersion)
            .IsRowVersion();

        builder.HasOne(ii => ii.Product)
            .WithOne(p => p.InventoryItem)
            .HasForeignKey<InventoryItem>(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one inventory item per product
        builder.HasIndex(ii => ii.ProductId)
            .IsUnique()
            .HasDatabaseName("IX_InventoryItems_ProductId");

        // Check constraint: quantities cannot be negative
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryItems_AvailableQuantity_NonNegative",
            "[AvailableQuantity] >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_InventoryItems_ReservedQuantity_NonNegative",
            "[ReservedQuantity] >= 0"));
    }
}
