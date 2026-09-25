using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.DTOs.Vehicle;

public class VehicleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public bool IsActive { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public string? AssignedDriverName { get; set; }
    public decimal? LastLatitude { get; set; }
    public decimal? LastLongitude { get; set; }
    public decimal? LastSpeedKmh { get; set; }
    public DateTime? LastRecordedAt { get; set; }
}

public class CreateVehicleRequest
{
    public string Name { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DriverId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public enum VehicleDeleteResult
{
    NotFound = 0,
    Deleted = 1,
    Deactivated = 2
}

public class UpdateVehicleRequest
{
    public string Name { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public bool IsActive { get; set; }
    public Guid? DriverId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class DriverDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleName { get; set; }
    public string? LicensePlate { get; set; }
}