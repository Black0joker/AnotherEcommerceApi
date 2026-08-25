using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class DiscountCategoryConfiguration : IEntityTypeConfiguration<DiscountCategory>
{
    public void Configure(EntityTypeBuilder<DiscountCategory> builder)
    {
        builder.ToTable("DiscountCategories");

        builder.HasKey(dc => new { dc.DiscountId, dc.CategoryId });

        builder.HasOne(dc => dc.Discount)
            .WithMany(d => d.DiscountCategories)
            .HasForeignKey(dc => dc.DiscountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dc => dc.Category)
            .WithMany(c => c.DiscountCategories)
            .HasForeignKey(dc => dc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(dc => dc.CategoryId)
            .HasDatabaseName("IX_DiscountCategories_CategoryId");
    }
}
