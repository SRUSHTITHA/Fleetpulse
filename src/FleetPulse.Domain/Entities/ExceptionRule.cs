using FleetPulse.Domain.Enums;

namespace FleetPulse.Domain.Entities;

public class ExceptionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ExceptionRuleType RuleType { get; set; }
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdPercent { get; set; }
    public decimal? ThresholdValue { get; set; }
    public ExceptionSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FleetException> Exceptions { get; set; } = new List<FleetException>();
}
