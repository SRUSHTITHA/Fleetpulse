namespace FleetPulse.Domain.Entities;

public class LocationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal? SpeedKmh { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Trip Trip { get; set; } = null!;
}
