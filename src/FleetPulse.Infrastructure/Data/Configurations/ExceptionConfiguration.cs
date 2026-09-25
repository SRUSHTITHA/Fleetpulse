using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetPulse.Infrastructure.Data.Configurations;

public class ExceptionConfiguration : IEntityTypeConfiguration<FleetException>
{
    public void Configure(EntityTypeBuilder<FleetException> builder)
    {
        builder.ToTable("exceptions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.RuleType).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Severity).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
        builder.Property(e => e.DetectedAt).HasColumnType("timestamptz");
        builder.Property(e => e.AcknowledgedAt).HasColumnType("timestamptz");
        builder.Property(e => e.ResolvedAt).HasColumnType("timestamptz");

        builder.HasIndex(e => new { e.Status, e.DetectedAt });
        builder.HasIndex(e => e.TripId);
        builder.HasIndex(e => new { e.TripId, e.RuleType });

        builder.HasOne(e => e.Trip)
            .WithMany(t => t.Exceptions)
            .HasForeignKey(e => e.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Rule)
            .WithMany(r => r.Exceptions)
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
