using System.Collections.Concurrent;
using FleetPulse.Application.DTOs.Live;
using FleetPulse.Application.DTOs.Location;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FleetPulse.Infrastructure.Services;

public class LocationService : ILocationService
{
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(2);
    private const int MaxThrottleEntries = 10_000;
    private static readonly TimeSpan ThrottleEntryTtl = TimeSpan.FromMinutes(1);

    private readonly ILocationRepository _locations;
    private readonly ITripRepository _trips;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationBroadcaster _broadcaster;
    private readonly IExceptionDetectionService _exceptionDetection;
    private readonly ILogger<LocationService> _logger;

    private static readonly ConcurrentDictionary<Guid, DateTime> _lastAcceptedByTrip = new();

    public LocationService(
        ILocationRepository locations,
        ITripRepository trips,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ILocationBroadcaster broadcaster,
        IExceptionDetectionService exceptionDetection,
        ILogger<LocationService> logger)
    {
        _locations = locations;
        _trips = trips;
        _users = users;
        _unitOfWork = unitOfWork;
        _broadcaster = broadcaster;
        _exceptionDetection = exceptionDetection;
        _logger = logger;
    }

    public async Task<ReportLocationResponse> ReportLocationAsync(Guid driverUserId, ReportLocationRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        // Drivers can reconnect mid-trip and resend nearly identical pings; this keeps
        // stale retries from creating a false trail while still accepting genuinely new fixes.
        if (_lastAcceptedByTrip.TryGetValue(request.TripId, out var lastAccepted) && now - lastAccepted < ThrottleInterval)
            return Failed(LocationReportStatus.Throttled, "Location report rejected — please wait at least 2 seconds between reports.");

        var trip = await _trips.GetByIdWithDetailsAsync(request.TripId, cancellationToken);
        if (trip is null)
            return Failed(LocationReportStatus.TripNotFound, "Trip not found.");

        var user = await _users.GetByIdAsync(driverUserId, includeDriver: true, cancellationToken);
        if (user?.Driver is null || user.Driver.Id != trip.DriverId)
            return Failed(LocationReportStatus.NotTripDriver, "You are not the driver of this trip.");

        if (trip.Status != TripStatus.InProgress)
            return Failed(LocationReportStatus.TripNotInProgress, "Trip is not in progress.");

        var location = new LocationHistory
        {
            TripId = trip.Id,
            DriverId = trip.DriverId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SpeedKmh = request.SpeedKmh,
            RecordedAt = now
        };

        await _locations.AddAsync(location, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Store the accepted timestamp only after the insert succeeds so a retry that was
        // rejected by the database does not permanently suppress valid follow-up reports.
        _lastAcceptedByTrip[trip.Id] = location.RecordedAt;
        SweepStaleThrottleEntries(now);

        await _broadcaster.BroadcastLocationAsync(new LocationBroadcastDto
        {
            TripId = trip.Id,
            DriverId = trip.DriverId,
            DriverName = trip.Driver?.User?.FullName ?? string.Empty,
            VehicleId = trip.VehicleId,
            VehicleName = trip.Vehicle?.Name ?? string.Empty,
            LicensePlate = trip.Vehicle?.LicensePlate ?? string.Empty,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            SpeedKmh = location.SpeedKmh,
            RecordedAt = location.RecordedAt
        });

        // Ingest hook: a new point can trigger/clear rule evaluations immediately (dedup makes
        // repeated cycles safe). Failures here must never fail an already-accepted location.
        try
        {
            await _exceptionDetection.EvaluateTripAsync(trip.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception evaluation failed for trip {TripId}.", trip.Id);
        }

        return new ReportLocationResponse
        {
            Status = LocationReportStatus.Accepted,
            Message = "Location recorded.",
            Location = Map(location)
        };
    }

    public async Task<IReadOnlyList<LocationDto>> GetTripHistoryAsync(Guid tripId, int take = 100, CancellationToken cancellationToken = default)
    {
        var locations = await _locations.GetRecentAsync(tripId, take, cancellationToken);
        return locations.Select(Map).ToList();
    }

    private void SweepStaleThrottleEntries(DateTime now)
    {
        if (_lastAcceptedByTrip.Count < MaxThrottleEntries)
            return;

        foreach (var (tripId, acceptedAt) in _lastAcceptedByTrip)
        {
            if (now - acceptedAt > ThrottleEntryTtl)
                _lastAcceptedByTrip.TryRemove(tripId, out _);
        }
    }

    private static ReportLocationResponse Failed(LocationReportStatus status, string message)
        => new() { Status = status, Message = message };

    private static LocationDto Map(LocationHistory location) => new()
    {
        Id = location.Id,
        TripId = location.TripId,
        DriverId = location.DriverId,
        Latitude = location.Latitude,
        Longitude = location.Longitude,
        SpeedKmh = location.SpeedKmh,
        RecordedAt = location.RecordedAt
    };
}