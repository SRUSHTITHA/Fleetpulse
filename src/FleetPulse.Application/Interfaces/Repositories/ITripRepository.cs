using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.Interfaces.Repositories;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Trip?> GetActiveByDriverAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trip>> GetByStatusAsync(TripStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trip>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task AddAsync(Trip trip, CancellationToken cancellationToken = default);
}