using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.DTOs.Live;

public class LocationBroadcastDto
{
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? SpeedKmh { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class ExceptionAlertDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public ExceptionRuleType RuleType { get; set; }
    public ExceptionSeverity Severity { get; set; }
    public ExceptionStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime DetectedAt { get; set; }
}