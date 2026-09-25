namespace FleetPulse.Application.Interfaces;

public sealed record SnappedPoint(decimal Latitude, decimal Longitude);

public interface IRoadSnapService
{
    /// <summary>
    /// Snaps a raw GPS fix to the nearest drivable road using Google's Roads API
    /// (nearestRoads). Returns null when no key is configured, the Roads API returns
    /// no road for the point, or the call fails — callers then fall back to the raw fix.
    /// </summary>
    Task<SnappedPoint?> TrySnapAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
}