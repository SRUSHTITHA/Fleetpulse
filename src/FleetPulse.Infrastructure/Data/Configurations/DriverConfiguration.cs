using FleetPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetPulse.Infrastructure.Data.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.Property(d => d.IsActive).IsRequired();

        builder.HasOne(d => d.Vehicle)
            .WithMany(v => v.Drivers)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
