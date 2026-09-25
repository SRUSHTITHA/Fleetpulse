using FleetPulse.Application.DTOs.Trip;

namespace FleetPulse.Application.Interfaces;

public interface ITripService
{
    Task<TripDto?> StartTripAsync(Guid driverId, StartTripRequest request, CancellationToken cancellationToken = default);
    Task<TripDto?> EndTripAsync(Guid driverId, Guid tripId, CancellationToken cancellationToken = default);
    Task<TripDto?> CancelTripAsync(Guid driverId, Guid tripId, CancellationToken cancellationToken = default);
    Task<TripDto?> GetActiveTripForDriverAsync(Guid driverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TripDto>> GetDashboardTripsAsync(CancellationToken cancellationToken = default);
}