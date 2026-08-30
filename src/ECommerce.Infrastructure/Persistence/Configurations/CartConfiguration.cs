using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasOne(c => c.User)
            .WithOne(u => u.Cart)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One cart per user
        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Carts_UserId");

        // Supports the hourly expired-cart cleanup range scan; without it
        // the cleanup job scans the entire Carts table on every tick.
        builder.HasIndex(c => c.ExpiresAt)
            .HasDatabaseName("IX_Carts_ExpiresAt");
    }
}
