using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
{
    public void Configure(EntityTypeBuilder<OrderAddress> builder)
    {
        builder.ToTable("OrderAddresses");

        builder.HasKey(oa => oa.Id);

        builder.Property(oa => oa.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oa => oa.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oa => oa.StreetLine1)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oa => oa.StreetLine2)
            .HasMaxLength(200);

        builder.Property(oa => oa.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oa => oa.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oa => oa.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(oa => oa.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(oa => oa.PhoneNumber)
            .HasMaxLength(30);

        builder.HasOne(oa => oa.Order)
            .WithOne(o => o.ShippingAddress)
            .HasForeignKey<OrderAddress>(oa => oa.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(oa => oa.OrderId)
            .IsUnique()
            .HasDatabaseName("IX_OrderAddresses_OrderId");
    }
}
