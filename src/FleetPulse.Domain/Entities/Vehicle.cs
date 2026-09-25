using FleetPulse.Domain.Enums;

namespace FleetPulse.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
