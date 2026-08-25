using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class DiscountProductConfiguration : IEntityTypeConfiguration<DiscountProduct>
{
    public void Configure(EntityTypeBuilder<DiscountProduct> builder)
    {
        builder.ToTable("DiscountProducts");

        builder.HasKey(dp => new { dp.DiscountId, dp.ProductId });

        builder.HasOne(dp => dp.Discount)
            .WithMany(d => d.DiscountProducts)
            .HasForeignKey(dp => dp.DiscountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dp => dp.Product)
            .WithMany(p => p.DiscountProducts)
            .HasForeignKey(dp => dp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dp => dp.ProductId)
            .HasDatabaseName("IX_DiscountProducts_ProductId");
    }
}
