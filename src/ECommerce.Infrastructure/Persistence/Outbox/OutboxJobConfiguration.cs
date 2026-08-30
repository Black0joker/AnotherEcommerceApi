using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public class OutboxJobConfiguration : IEntityTypeConfiguration<OutboxJob>
{
    public void Configure(EntityTypeBuilder<OutboxJob> builder)
    {
        builder.ToTable("OutboxJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .ValueGeneratedOnAdd();

        builder.Property(j => j.JobType)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.PayloadJson)
            .IsRequired();

        builder.Property(j => j.CreatedAtUtc)
            .IsRequired();

        builder.Property(j => j.LastError)
            .HasMaxLength(2000);

        // Pending-jobs polling (WHERE ProcessedAtUtc IS NULL ORDER BY Id)
        // seeks on this filtered index; processed rows fall out of it.
        builder.HasIndex(j => j.Id)
            .HasDatabaseName("IX_OutboxJobs_Pending")
            .HasFilter("[ProcessedAtUtc] IS NULL");
    }
}
