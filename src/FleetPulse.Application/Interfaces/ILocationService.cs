using FleetPulse.Application.DTOs.Location;

namespace FleetPulse.Application.Interfaces;

public interface ILocationService
{
    Task<ReportLocationResponse> ReportLocationAsync(Guid driverId, ReportLocationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocationDto>> GetTripHistoryAsync(Guid tripId, int take = 100, CancellationToken cancellationToken = default);
}