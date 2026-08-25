using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pi => pi.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pi => pi.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pi => pi.Url)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pi => pi.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");
    }
}
