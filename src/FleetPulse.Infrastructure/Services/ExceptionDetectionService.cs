using System.Collections.Concurrent;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Infrastructure.Services;

public class ExceptionDetectionService : IExceptionDetectionService
{
    private const double StopRadiusMeters = 50.0;
    private const int DefaultLookbackMinutes = 30;

    /// <summary>
    /// Serializes evaluation per trip so the ingest hook (request scope) and the periodic
    /// engine (hosted-service scope) can't both race the (trip, rule) dedup check.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> TripGates = new();

    private readonly ITripRepository _trips;
    private readonly ILocationRepository _locations;
    private readonly IExceptionRuleRepository _rules;
    private readonly IExceptionRepository _exceptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationBroadcaster _broadcaster;
    private readonly IRoadSnapService _roadSnap;

    public ExceptionDetectionService(
        ITripRepository trips,
        ILocationRepository locations,
        IExceptionRuleRepository rules,
        IExceptionRepository exceptions,
        IUnitOfWork unitOfWork,
        ILocationBroadcaster broadcaster,
        IRoadSnapService roadSnap)
    {
        _trips = trips;
        _locations = locations;
        _rules = rules;
        _exceptions = exceptions;
        _unitOfWork = unitOfWork;
        _broadcaster = broadcaster;
        _roadSnap = roadSnap;
    }

    public async Task EvaluateTripAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var gate = TripGates.GetOrAdd(tripId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await EvaluateCoreAsync(tripId, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task EvaluateCoreAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await _trips.GetByIdWithDetailsAsync(tripId, ct);
        if (trip is null || trip.Status != TripStatus.InProgress)
            return;

        var rules = await _rules.GetEnabledAsync(ct);
        if (rules.Count == 0)
            return;

        var since = DateTime.UtcNow.AddMinutes(-MaxLookbackMinutes(rules));
        var points = await _locations.GetSinceAsync(tripId, since, ct);
        if (points.Count == 0)
            return;

        var latest = points[^1];
        var created = new List<FleetException>();

        foreach (var rule in rules)
        {
            var decision = await TryEvaluate(rule, trip, points, latest);
            var existing = await _exceptions.GetOpenByTripAndRuleAsync(tripId, rule.RuleType, ct);

            if (decision.Fired)
            {
                if (existing is null)
                {
                    var exception = new FleetException
                    {
                        TripId = trip.Id,
                        DriverId = trip.DriverId,
                        RuleId = rule.Id,
                        RuleType = rule.RuleType,
                        Severity = rule.Severity,
                        Status = ExceptionStatus.Active,
                        Title = TitleFor(rule.RuleType),
                        Description = decision.Description,
                        Latitude = decision.Latitude,
                        Longitude = decision.Longitude,
                        DetectedAt = latest.RecordedAt,
                        Trip = trip
                    };

                    await _exceptions.AddAsync(exception, ct);
                    created.Add(exception);
                }
                else
                {
                    // Dedup: same (trip, rule) still firing -> refresh, never a second alert.
                    existing.DetectedAt = latest.RecordedAt;
                    existing.Description = decision.Description;
                    existing.Latitude = decision.Latitude;
                    existing.Longitude = decision.Longitude;
                    existing.Status = ExceptionStatus.Active;
                }
            }
            else if (existing is not null)
            {
                // Rule stopped firing -> close the open exception quietly.
                existing.Status = ExceptionStatus.Resolved;
                existing.ResolvedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        foreach (var exception in created)
            await _broadcaster.BroadcastExceptionAsync(ExceptionService.ToAlert(exception));
    }

    private async Task<RuleDecision> TryEvaluate(
        ExceptionRule rule,
        Trip trip,
        IReadOnlyList<LocationHistory> points,
        LocationHistory latest)
    {
        switch (rule.RuleType)
        {
            case ExceptionRuleType.StopTooLong:
                return RuleDecision.Create(TryEvaluateStopTooLong(rule, points, latest, out var stopDescription), stopDescription, latest);
            case ExceptionRuleType.DeliveryTrendingLate:
                return RuleDecision.Create(TryEvaluateTrendingLate(rule, trip, latest, out var lateDescription), lateDescription, latest);
            case ExceptionRuleType.RouteDeviation:
                var (fired, deviationDescription) = await TryEvaluateRouteDeviation(rule, trip, latest);
                return RuleDecision.Create(fired, deviationDescription, latest);
            case ExceptionRuleType.SpeedAnomaly:
                return RuleDecision.Create(TryEvaluateSpeedAnomaly(rule, latest, out var speedDescription), speedDescription, latest);
            default:
                return RuleDecision.Create(false, string.Empty, latest);
        }
    }

    private static bool TryEvaluateStopTooLong(ExceptionRule rule, IReadOnlyList<LocationHistory> points, LocationHistory latest, out string description)
    {
        description = string.Empty;
        var thresholdMinutes = rule.ThresholdMinutes ?? 10;

        // Walk backwards from the latest point while it stays within StopRadius of the anchor;
        // the cluster's span is "stopped this long".
        var clusterStart = points.Count - 1;
        for (var i = points.Count - 2; i >= 0; i--)
        {
            if (HaversineMeters(points[i], latest) <= StopRadiusMeters)
                clusterStart = i;
            else
                break;
        }

        var spanMinutes = (latest.RecordedAt - points[clusterStart].RecordedAt).TotalMinutes;
        if (spanMinutes < thresholdMinutes)
            return false;

        description = $"Vehicle has remained within {StopRadiusMeters:0} m for {spanMinutes:0} minutes (threshold {thresholdMinutes} min).";
        return true;
    }

    private static bool TryEvaluateTrendingLate(ExceptionRule rule, Trip trip, LocationHistory latest, out string description)
    {
        description = string.Empty;
        var thresholdPercent = (double)(rule.ThresholdPercent ?? 15);

        var start = trip.ActualDeparture ?? trip.EstimatedDeparture;
        var elapsed = (latest.RecordedAt - start).TotalMinutes;
        var totalTime = (trip.EstimatedArrival - trip.EstimatedDeparture).TotalMinutes;
        if (totalTime <= 0 || elapsed <= 0)
            return false;

        var totalDistance = HaversineMeters(trip.OriginLat, trip.OriginLng, trip.DestLat, trip.DestLng);
        if (totalDistance <= 0)
            return false;

        var coveredDistance = HaversineMeters(trip.OriginLat, trip.OriginLng, latest.Latitude, latest.Longitude);

        var timeProgress = elapsed / totalTime;
        var distanceProgress = coveredDistance / totalDistance;
        var deficitPercent = (timeProgress - distanceProgress) * 100.0;

        if (deficitPercent < thresholdPercent)
            return false;

        description = $"Only {distanceProgress * 100:0}% of the route covered after {timeProgress * 100:0}% of allotted time (threshold {thresholdPercent:0}%).";
        return true;
    }

    private async Task<(bool Fired, string Description)> TryEvaluateRouteDeviation(ExceptionRule rule, Trip trip, LocationHistory latest)
    {
        if (trip.OriginLat == trip.DestLat && trip.OriginLng == trip.DestLng)
            return (false, string.Empty);

        var thresholdMeters = (double)(rule.ThresholdValue ?? 500);

        // Snap the raw GPS fix onto the nearest road first so a jittery fix beside the
        // planned corridor (e.g. a field next to the highway) doesn't look like a deviation.
        // When no key is configured / no road is matched / Google is unreachable, the raw
        // fix is used and the original perpendicular-corridor check still applies.
        var snapped = await _roadSnap.TrySnapAsync(latest.Latitude, latest.Longitude);
        var nearLat = snapped?.Latitude ?? latest.Latitude;
        var nearLng = snapped?.Longitude ?? latest.Longitude;

        var deviation = PointToSegmentMeters(
            trip.OriginLat, trip.OriginLng,
            trip.DestLat, trip.DestLng,
            nearLat, nearLng);

        if (deviation <= thresholdMeters)
            return (false, string.Empty);

        return (true, $"Vehicle is {deviation:0} m from the planned origin-to-destination corridor (threshold {thresholdMeters:0} m).");
    }

    private static bool TryEvaluateSpeedAnomaly(ExceptionRule rule, LocationHistory latest, out string description)
    {
        description = string.Empty;
        if (latest.SpeedKmh is not decimal speed)
            return false; // no speed data reported -> nothing to evaluate

        var thresholdKmh = rule.ThresholdValue ?? 120;
        if (speed <= thresholdKmh)
            return false;

        description = $"Speed {speed:0} km/h exceeds configured limit of {thresholdKmh:0} km/h.";
        return true;
    }

    private static int MaxLookbackMinutes(IReadOnlyList<ExceptionRule> rules)
    {
        var max = DefaultLookbackMinutes;
        foreach (var rule in rules)
        {
            if (rule.RuleType == ExceptionRuleType.StopTooLong && rule.ThresholdMinutes is > 0 && rule.ThresholdMinutes.Value > max)
                max = rule.ThresholdMinutes.Value;
        }

        return max + 5; // buffer so a stationary cluster spanning the threshold is fully within the window
    }

    private static string TitleFor(ExceptionRuleType ruleType) => ruleType switch
    {
        ExceptionRuleType.StopTooLong => "Vehicle stopped too long",
        ExceptionRuleType.DeliveryTrendingLate => "Delivery trending late",
        ExceptionRuleType.RouteDeviation => "Route deviation detected",
        ExceptionRuleType.SpeedAnomaly => "Speed anomaly detected",
        _ => "Exception detected"
    };

    private static double HaversineMeters(LocationHistory a, LocationHistory b)
        => HaversineMeters(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    private static double HaversineMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double earthRadiusMeters = 6_371_000.0;

        var phi1 = ToRadians((double)lat1);
        var phi2 = ToRadians((double)lat2);
        var dPhi = ToRadians((double)lat2 - (double)lat1);
        var dLambda = ToRadians((double)lng2 - (double)lng1);

        var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2)
              + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    /// <summary>
    /// Perpendicular distance from a point to the origin-destination segment (clamped so points
    /// near the endpoints aren't flagged), using a local equirectangular projection.
    /// </summary>
    private static double PointToSegmentMeters(decimal aLat, decimal aLng, decimal bLat, decimal bLng, decimal pLat, decimal pLng)
    {
        var metersPerDegLat = 111_320.0;
        var metersPerDegLng = metersPerDegLat * Math.Cos(ToRadians((double)aLat));

        double ax = (double)aLng * metersPerDegLng, ay = (double)aLat * metersPerDegLat;
        double bx = (double)bLng * metersPerDegLng, by = (double)bLat * metersPerDegLat;
        double px = (double)pLng * metersPerDegLng, py = (double)pLat * metersPerDegLat;

        double dx = bx - ax, dy = by - ay;
        var lenSq = dx * dx + dy * dy;
        if (lenSq < 1e-9)
            return HaversineMeters(aLat, aLng, pLat, pLng);

        var t = ((px - ax) * dx + (py - ay) * dy) / lenSq;
        t = Math.Max(0.0, Math.Min(1.0, t));

        var collisionPointX = ax + t * dx;
        var collisionPointY = ay + t * dy;
        var offX = px - collisionPointX;
        var offY = py - collisionPointY;
        return Math.Sqrt(offX * offX + offY * offY);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private readonly record struct RuleDecision(bool Fired, string Description, decimal Latitude, decimal Longitude)
    {
        public static RuleDecision Create(bool fired, string description, LocationHistory point)
            => new(fired, description, point.Latitude, point.Longitude);
    }
}