using FleetPulse.Application.DTOs.Geo;

namespace FleetPulse.Application.Interfaces;

public interface IGeoService
{
    Task<GeocodeResultDto?> GeocodeAsync(string query, CancellationToken cancellationToken = default);

    Task<RouteResultDto> GetRouteAsync(
        double originLat,
        double originLng,
        double destLat,
        double destLng,
        CancellationToken cancellationToken = default);
}
