namespace FleetPulse.Application.DTOs.Location;

public class ReportLocationRequest
{
    public Guid TripId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? SpeedKmh { get; set; }
}

public enum LocationReportStatus
{
    Accepted,
    Throttled,
    TripNotFound,
    NotTripDriver,
    TripNotInProgress
}

public class ReportLocationResponse
{
    public LocationReportStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public LocationDto? Location { get; set; }
}

public class LocationDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? SpeedKmh { get; set; }
    public DateTime RecordedAt { get; set; }
}