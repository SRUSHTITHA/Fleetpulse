using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetPulse.Infrastructure.Data.Configurations;

public class ExceptionRuleConfiguration : IEntityTypeConfiguration<ExceptionRule>
{
    public void Configure(EntityTypeBuilder<ExceptionRule> builder)
    {
        builder.ToTable("exception_rules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.RuleType).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(32);
        builder.Property(r => r.ThresholdPercent).HasColumnType("decimal(5,2)");
        builder.Property(r => r.ThresholdValue).HasColumnType("decimal(9,2)");
        builder.Property(r => r.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(r => r.RuleType);
    }
}
