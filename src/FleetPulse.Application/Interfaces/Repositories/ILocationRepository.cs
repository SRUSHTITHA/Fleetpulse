using FleetPulse.Domain.Entities;

namespace FleetPulse.Application.Interfaces.Repositories;

public interface ILocationRepository
{
    Task AddAsync(LocationHistory location, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationHistory>> GetRecentAsync(Guid tripId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationHistory>> GetSinceAsync(Guid tripId, DateTime since, CancellationToken cancellationToken = default);
}