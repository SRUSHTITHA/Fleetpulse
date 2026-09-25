using FleetPulse.Domain.Enums;

namespace FleetPulse.Domain.Entities;

public class FleetException
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public Guid RuleId { get; set; }
    public ExceptionRuleType RuleType { get; set; }
    public ExceptionSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public ExceptionStatus Status { get; set; }

    public Trip Trip { get; set; } = null!;
    public ExceptionRule Rule { get; set; } = null!;
}
