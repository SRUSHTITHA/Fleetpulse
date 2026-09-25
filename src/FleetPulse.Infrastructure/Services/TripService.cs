using FleetPulse.Application.DTOs.Trip;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Infrastructure.Services;

public class TripService : ITripService
{
    private readonly ITripRepository _trips;
    private readonly IUserRepository _users;
    private readonly IVehicleRepository _vehicles;
    private readonly IUnitOfWork _unitOfWork;

    public TripService(
        ITripRepository trips,
        IUserRepository users,
        IVehicleRepository vehicles,
        IUnitOfWork unitOfWork)
    {
        _trips = trips;
        _users = users;
        _vehicles = vehicles;
        _unitOfWork = unitOfWork;
    }

    public async Task<TripDto?> StartTripAsync(Guid driverUserId, StartTripRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(driverUserId, includeDriver: true, cancellationToken);
        if (user?.Driver is null)
            return null;

        var driver = user.Driver;

        if (await _trips.GetActiveByDriverAsync(driver.Id, cancellationToken) is not null)
            return null;

        Vehicle? vehicle = driver.VehicleId is { } assignedVehicleId
            ? await _vehicles.GetByIdAsync(assignedVehicleId, cancellationToken)
            : null;

        if (vehicle is null || !vehicle.IsActive)
        {
            driver.VehicleId = null;
            vehicle = (await _vehicles.GetActiveAsync(cancellationToken)).FirstOrDefault();
            if (vehicle is not null)
                driver.VehicleId = vehicle.Id;
        }

        if (driver.VehicleId is null)
            return null;

        var now = DateTime.UtcNow;
        var trip = new Trip
        {
            DriverId = driver.Id,
            VehicleId = driver.VehicleId.Value,
            Driver = driver,
            Vehicle = vehicle,
            Status = TripStatus.InProgress,
            Origin = request.Origin,
            Destination = request.Destination,
            OriginLat = request.OriginLat,
            OriginLng = request.OriginLng,
            DestLat = request.DestLat,
            DestLng = request.DestLng,
            EstimatedDeparture = now,
            ActualDeparture = now,
            EstimatedArrival = request.EstimatedArrival
        };

        await _trips.AddAsync(trip, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(trip);
    }

    public async Task<TripDto?> EndTripAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken = default)
        => await SetTripStatusAsync(driverUserId, tripId, TripStatus.Completed, recordActualArrival: true, cancellationToken);

    public async Task<TripDto?> CancelTripAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken = default)
        => await SetTripStatusAsync(driverUserId, tripId, TripStatus.Cancelled, recordActualArrival: false, cancellationToken);

    public async Task<TripDto?> GetActiveTripForDriverAsync(Guid driverUserId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(driverUserId, includeDriver: true, cancellationToken);
        if (user?.Driver is null)
            return null;

        var trip = await _trips.GetActiveByDriverAsync(user.Driver.Id, cancellationToken);
        return trip is null ? null : Map(trip);
    }

    public async Task<IReadOnlyList<TripDto>> GetDashboardTripsAsync(CancellationToken cancellationToken = default)
    {
        var trips = await _trips.GetAllWithDetailsAsync(cancellationToken);

        var orphanedTrips = trips
            .Where(trip => trip.Status == TripStatus.InProgress && !trip.Vehicle.IsActive)
            .ToList();
        foreach (var trip in orphanedTrips)
            trip.Status = TripStatus.Cancelled;

        if (orphanedTrips.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return trips.Select(Map).ToList();
    }

    private async Task<Trip?> GetOwnedTripAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(driverUserId, includeDriver: true, cancellationToken);
        if (user?.Driver is null)
            return null;

        var trip = await _trips.GetByIdWithDetailsAsync(tripId, cancellationToken);
        if (trip is null || trip.DriverId != user.Driver.Id)
            return null;

        return trip;
    }

    private async Task<TripDto?> SetTripStatusAsync(Guid driverUserId, Guid tripId, TripStatus status, bool recordActualArrival, CancellationToken cancellationToken)
    {
        var trip = await GetOwnedTripAsync(driverUserId, tripId, cancellationToken);
        if (trip is null)
            return null;

        trip.Status = status;
        if (recordActualArrival)
            trip.ActualArrival = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(trip);
    }

    private static TripDto Map(Trip trip)
    {
        var last = trip.Locations.FirstOrDefault();
        return new TripDto
        {
            Id = trip.Id,
            DriverId = trip.DriverId,
            VehicleId = trip.VehicleId,
            Status = trip.Status,
            Origin = trip.Origin,
            Destination = trip.Destination,
            OriginLat = trip.OriginLat,
            OriginLng = trip.OriginLng,
            DestLat = trip.DestLat,
            DestLng = trip.DestLng,
            EstimatedDeparture = trip.EstimatedDeparture,
            ActualDeparture = trip.ActualDeparture,
            EstimatedArrival = trip.EstimatedArrival,
            ActualArrival = trip.ActualArrival,
            CreatedAt = trip.CreatedAt,
            DriverName = trip.Driver?.User?.FullName,
            VehicleName = trip.Vehicle?.Name,
            LicensePlate = trip.Vehicle?.LicensePlate,
            LastLatitude = last?.Latitude,
            LastLongitude = last?.Longitude,
            LastSpeedKmh = last?.SpeedKmh,
            LastRecordedAt = last?.RecordedAt
        };
    }
}