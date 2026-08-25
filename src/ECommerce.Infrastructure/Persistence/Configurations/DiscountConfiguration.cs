using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("Discounts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Value)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(d => d.MinimumOrderValue)
            .HasPrecision(18, 2);

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasDatabaseName("IX_Discounts_Code");

        builder.HasIndex(d => d.IsActive)
            .HasDatabaseName("IX_Discounts_IsActive");

        builder.HasIndex(d => new { d.StartsAt, d.EndsAt })
            .HasDatabaseName("IX_Discounts_DateRange");
    }
}
