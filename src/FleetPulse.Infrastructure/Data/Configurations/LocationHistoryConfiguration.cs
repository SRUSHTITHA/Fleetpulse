using FleetPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetPulse.Infrastructure.Data.Configurations;

public class LocationHistoryConfiguration : IEntityTypeConfiguration<LocationHistory>
{
    public void Configure(EntityTypeBuilder<LocationHistory> builder)
    {
        builder.ToTable("location_history");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(l => l.Longitude).HasColumnType("decimal(9,6)");
        builder.Property(l => l.SpeedKmh).HasColumnType("decimal(6,2)");
        builder.Property(l => l.RecordedAt).HasColumnType("timestamptz");

        builder.HasIndex(l => new { l.TripId, l.RecordedAt });
        builder.HasIndex(l => l.DriverId);

        builder.HasOne(l => l.Trip)
            .WithMany(t => t.Locations)
            .HasForeignKey(l => l.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
