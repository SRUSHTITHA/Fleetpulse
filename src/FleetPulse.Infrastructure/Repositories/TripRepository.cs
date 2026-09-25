using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly FleetPulseDbContext _db;

    public TripRepository(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Trips.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Trip?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await WithDetails(_db.Trips)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<Trip?> GetActiveByDriverAsync(Guid driverId, CancellationToken cancellationToken = default)
        => await WithDetails(_db.Trips)
            .Where(t => t.DriverId == driverId && t.Status == TripStatus.InProgress && t.Vehicle.IsActive)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Trip>> GetByStatusAsync(TripStatus status, CancellationToken cancellationToken = default)
        => await _db.Trips
            .Where(t => t.Status == status)
            .OrderBy(t => t.EstimatedArrival)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Trip>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        => await WithDetails(_db.Trips)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> GetCountByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
        => _db.Trips.CountAsync(t => t.VehicleId == vehicleId, cancellationToken);

    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        await _db.Trips.AddAsync(trip, cancellationToken);
    }

    private static IQueryable<Trip> WithDetails(IQueryable<Trip> query)
        => query
            .Include(t => t.Driver).ThenInclude(d => d.User)
            .Include(t => t.Vehicle)
            .Include(t => t.Locations.OrderByDescending(l => l.RecordedAt).Take(1));
}