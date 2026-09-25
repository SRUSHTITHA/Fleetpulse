using FleetPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Data;

public class FleetPulseDbContext : DbContext
{
    public FleetPulseDbContext(DbContextOptions<FleetPulseDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<LocationHistory> LocationHistory => Set<LocationHistory>();
    public DbSet<ExceptionRule> ExceptionRules => Set<ExceptionRule>();
    public DbSet<FleetException> Exceptions => Set<FleetException>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetPulseDbContext).Assembly);
    }
}
