namespace FleetPulse.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? VehicleId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
