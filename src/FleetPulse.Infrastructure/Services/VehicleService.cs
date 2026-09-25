using FleetPulse.Application.DTOs.Vehicle;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicles;
    private readonly IUserRepository _users;
    private readonly ITripRepository _trips;
    private readonly IUnitOfWork _unitOfWork;

    public VehicleService(
        IVehicleRepository vehicles,
        IUserRepository users,
        ITripRepository trips,
        IUnitOfWork unitOfWork)
    {
        _vehicles = vehicles;
        _users = users;
        _trips = trips;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<VehicleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicles.GetAllWithDetailsAsync(cancellationToken);
        return vehicles.Select(Map).ToList();
    }

    public async Task<VehicleDto?> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = new Vehicle();
        ApplyDetails(vehicle, request.Name, request.LicensePlate, request.Type, request.IsActive, request.Latitude, request.Longitude);

        if (request.DriverId is not null)
        {
            if (!await TryAssignDriverAsync(vehicle, request.DriverId.Value, cancellationToken))
                return null;
        }

        await _vehicles.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _vehicles.GetByIdWithDetailsAsync(vehicle.Id, cancellationToken);
        return saved is null ? null : Map(saved);
    }

    public async Task<VehicleDto?> UpdateAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicles.GetByIdWithDetailsAsync(vehicleId, cancellationToken);
        if (vehicle is null)
            return null;

        ApplyDetails(vehicle, request.Name, request.LicensePlate, request.Type, request.IsActive, request.Latitude, request.Longitude);

        if (!vehicle.IsActive)
            await CancelActiveTripsAsync(vehicleId, cancellationToken);

        if (request.DriverId is null)
        {
            // An update that clears the driver always un-assigns whoever is currently
            // assigned, but never blocks on an absent driver.
            await UnassignVehicleDriversAsync(vehicleId, cancellationToken);
        }
        else if (!await TryAssignDriverAsync(vehicle, request.DriverId.Value, cancellationToken))
        {
            return null;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(vehicle);
    }

    public async Task<VehicleDeleteResult> DeleteAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicles.GetByIdAsync(vehicleId, cancellationToken);
        if (vehicle is null)
            return VehicleDeleteResult.NotFound;

        await CancelActiveTripsAsync(vehicleId, cancellationToken);

        await UnassignVehicleDriversAsync(vehicleId, cancellationToken);

        var tripCount = await _trips.GetCountByVehicleAsync(vehicleId, cancellationToken);
        if (tripCount == 0)
        {
            // No historical trips -> physically remove the vehicle.
            await _vehicles.DeleteAsync(vehicle, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return VehicleDeleteResult.Deleted;
        }

        // A vehicle with trip history can't be dropped (Trip.VehicleId FK is Restrict),
        // so deactivate it instead. It disappears from the active list but its history sticks.
        vehicle.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return VehicleDeleteResult.Deactivated;
    }

    public async Task<IReadOnlyList<DriverDto>> GetDriversAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicles.GetAllAsync(cancellationToken);
        var vehicleById = vehicles.ToDictionary(v => v.Id);

        var users = await GetUsersWithDriversAsync(cancellationToken);
        var drivers = users
            .Select(u => new DriverDto
            {
                Id = u.Driver!.Id,
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                LicenseNumber = u.Driver.LicenseNumber,
                IsActive = u.Driver.IsActive,
                VehicleId = u.Driver.VehicleId,
                VehicleName = u.Driver.VehicleId is { } vid && vehicleById.TryGetValue(vid, out var v) ? v.Name : null,
                LicensePlate = u.Driver.VehicleId is { } vid2 && vehicleById.TryGetValue(vid2, out var v2) ? v2.LicensePlate : null
            })
            .OrderBy(d => d.FullName)
            .ToList();

        return drivers;
    }

    private static void ApplyDetails(
        Vehicle vehicle,
        string name,
        string licensePlate,
        VehicleType type,
        bool isActive,
        decimal? latitude,
        decimal? longitude)
    {
        vehicle.Name = name.Trim();
        vehicle.LicensePlate = licensePlate.Trim().ToUpperInvariant();
        vehicle.Type = type;
        vehicle.IsActive = isActive;
        vehicle.Latitude = latitude;
        vehicle.Longitude = longitude;
    }

    private async Task<List<User>> GetUsersWithDriversAsync(CancellationToken cancellationToken)
        => (await _users.GetAllAsync(includeDriver: true, cancellationToken))
            .Where(u => u.Driver is not null)
            .ToList();

    /// <summary>
    /// Assigns <paramref name="driverId"/> to <paramref name="vehicle"/>, releasing the vehicle
    /// from any other driver and the driver from any other vehicle (no double-booking).
    /// </summary>
    private async Task<bool> TryAssignDriverAsync(Vehicle vehicle, Guid driverId, CancellationToken cancellationToken)
    {
        var drivers = (await GetUsersWithDriversAsync(cancellationToken))
            .Select(u => u.Driver!)
            .ToList();

        var target = drivers.FirstOrDefault(d => d.Id == driverId);
        if (target is null || !target.IsActive)
            return false;

        // A vehicle can have only one active driver, and a driver can only be assigned to one
        // vehicle at a time. Clear the old pairing before moving the target over.
        foreach (var driver in drivers.Where(d => d.VehicleId == vehicle.Id))
            driver.VehicleId = null;
        if (target.VehicleId != vehicle.Id)
            target.VehicleId = vehicle.Id;

        return true;
    }

    private async Task UnassignVehicleDriversAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var drivers = (await GetUsersWithDriversAsync(cancellationToken))
            .Select(u => u.Driver!)
            .Where(d => d.VehicleId == vehicleId)
            .ToList();

        foreach (var driver in drivers)
            driver.VehicleId = null;
    }

    private async Task CancelActiveTripsAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var activeTrips = await _trips.GetByStatusAsync(TripStatus.InProgress, cancellationToken);
        foreach (var trip in activeTrips.Where(t => t.VehicleId == vehicleId))
            trip.Status = TripStatus.Cancelled;
    }

    private static VehicleDto Map(Vehicle vehicle)
    {
        var assigned = vehicle.Drivers.FirstOrDefault();

        LocationHistory? lastLocation = null;
        var lastRecordedAt = (DateTime?)null;
        foreach (var driver in vehicle.Drivers)
        {
            foreach (var trip in driver.Trips.Where(trip => trip.Status == TripStatus.InProgress))
            {
                foreach (var loc in trip.Locations)
                {
                    if (lastRecordedAt is null || loc.RecordedAt > lastRecordedAt)
                    {
                        lastRecordedAt = loc.RecordedAt;
                        lastLocation = loc;
                    }
                }
            }
        }

        return new VehicleDto
        {
            Id = vehicle.Id,
            Name = vehicle.Name,
            LicensePlate = vehicle.LicensePlate,
            Type = vehicle.Type,
            IsActive = vehicle.IsActive,
            Latitude = vehicle.Latitude,
            Longitude = vehicle.Longitude,
            AssignedDriverId = assigned?.Id,
            AssignedDriverName = assigned?.User?.FullName,
            LastLatitude = lastLocation?.Latitude ?? vehicle.Latitude,
            LastLongitude = lastLocation?.Longitude ?? vehicle.Longitude,
            LastSpeedKmh = lastLocation?.SpeedKmh,
            LastRecordedAt = lastLocation?.RecordedAt
        };
    }
}