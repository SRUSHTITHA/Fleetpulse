using FleetPulse.Domain.Enums;

namespace FleetPulse.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Driver Driver { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<LocationHistory> Locations { get; set; } = new List<LocationHistory>();
    public ICollection<FleetException> Exceptions { get; set; } = new List<FleetException>();
}
