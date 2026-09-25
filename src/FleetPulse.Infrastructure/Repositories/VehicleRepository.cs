using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly FleetPulseDbContext _db;

    public VehicleRepository(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Vehicles.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<Vehicle?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Vehicles
            .Include(v => v.Drivers)
                .ThenInclude(d => d.User)
            .Include(v => v.Drivers)
                .ThenInclude(d => d.Trips)
                .ThenInclude(t => t.Locations)
            .SingleOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Vehicles
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        => await _db.Vehicles
            .OrderBy(v => v.Name)
            .Include(v => v.Drivers)
                .ThenInclude(d => d.User)
            .Include(v => v.Drivers)
                .ThenInclude(d => d.Trips)
                .ThenInclude(t => t.Locations)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _db.Vehicles
            .Where(v => v.IsActive && !v.Drivers.Any(d => d.IsActive))
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _db.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _db.Vehicles.Remove(vehicle);
        return Task.CompletedTask;
    }
}