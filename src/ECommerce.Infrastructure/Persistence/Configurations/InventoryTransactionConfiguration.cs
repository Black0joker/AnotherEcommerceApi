using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");

        builder.HasKey(it => it.Id);

        builder.Property(it => it.Quantity)
            .IsRequired();

        builder.Property(it => it.Reference)
            .HasMaxLength(200);

        builder.Property(it => it.Notes)
            .HasMaxLength(1000);

        builder.Property(it => it.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(it => it.InventoryItem)
            .WithMany(ii => ii.Transactions)
            .HasForeignKey(it => it.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(it => it.Product)
            .WithMany()
            .HasForeignKey(it => it.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(it => it.InventoryItemId)
            .HasDatabaseName("IX_InventoryTransactions_InventoryItemId");

        // Indexes
        builder.HasIndex(it => it.ProductId)
            .HasDatabaseName("IX_InventoryTransactions_ProductId");

        builder.HasIndex(it => it.CreatedAt)
            .HasDatabaseName("IX_InventoryTransactions_CreatedAt");

        builder.HasIndex(it => it.Type)
            .HasDatabaseName("IX_InventoryTransactions_Type");
    }
}
