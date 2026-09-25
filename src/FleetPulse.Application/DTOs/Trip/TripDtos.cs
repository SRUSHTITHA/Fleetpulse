using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.DTOs.Trip;

public class StartTripRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal OriginLat { get; set; }
    public decimal OriginLng { get; set; }
    public decimal DestLat { get; set; }
    public decimal DestLng { get; set; }
    public DateTime EstimatedArrival { get; set; }
}

public class TripIdRequest
{
    public Guid TripId { get; set; }
}

public class TripDto
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
    public TripStatus Status { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal OriginLat { get; set; }
    public decimal OriginLng { get; set; }
    public decimal DestLat { get; set; }
    public decimal DestLng { get; set; }
    public DateTime EstimatedDeparture { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public DateTime EstimatedArrival { get; set; }
    public DateTime? ActualArrival { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DriverName { get; set; }
    public string? VehicleName { get; set; }
    public string? LicensePlate { get; set; }
    public decimal? LastLatitude { get; set; }
    public decimal? LastLongitude { get; set; }
    public decimal? LastSpeedKmh { get; set; }
    public DateTime? LastRecordedAt { get; set; }
}