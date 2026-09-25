using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetPulse.Infrastructure.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).HasMaxLength(100).IsRequired();
        builder.Property(v => v.LicensePlate).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(v => v.IsActive).IsRequired();
    }
}
