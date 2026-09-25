using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetPulse.Infrastructure.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Origin).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Destination).HasMaxLength(255).IsRequired();
        builder.Property(t => t.OriginLat).HasColumnType("decimal(9,6)");
        builder.Property(t => t.OriginLng).HasColumnType("decimal(9,6)");
        builder.Property(t => t.DestLat).HasColumnType("decimal(9,6)");
        builder.Property(t => t.DestLng).HasColumnType("decimal(9,6)");
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.EstimatedDeparture).HasColumnType("timestamptz");
        builder.Property(t => t.ActualDeparture).HasColumnType("timestamptz");
        builder.Property(t => t.EstimatedArrival).HasColumnType("timestamptz");
        builder.Property(t => t.ActualArrival).HasColumnType("timestamptz");
        builder.Property(t => t.CreatedAt).HasColumnType("timestamptz");

        builder.HasOne(t => t.Driver)
            .WithMany(d => d.Trips)
            .HasForeignKey(t => t.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Vehicle)
            .WithMany(v => v.Trips)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
