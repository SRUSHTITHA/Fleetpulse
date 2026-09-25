using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly FleetPulseDbContext _db;

    public LocationRepository(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(LocationHistory location, CancellationToken cancellationToken = default)
    {
        await _db.LocationHistory.AddAsync(location, cancellationToken);
    }

    public async Task<IReadOnlyList<LocationHistory>> GetRecentAsync(Guid tripId, int take, CancellationToken cancellationToken = default)
        => await _db.LocationHistory
            .Where(l => l.TripId == tripId)
            .OrderByDescending(l => l.RecordedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LocationHistory>> GetSinceAsync(Guid tripId, DateTime since, CancellationToken cancellationToken = default)
        => await _db.LocationHistory
            .Where(l => l.TripId == tripId && l.RecordedAt >= since)
            .OrderBy(l => l.RecordedAt)
            .ToListAsync(cancellationToken);
}