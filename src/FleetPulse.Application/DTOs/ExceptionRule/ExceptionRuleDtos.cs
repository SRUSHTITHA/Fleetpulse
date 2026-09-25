using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.DTOs.ExceptionRule;

public class ExceptionRuleDto
{
    public Guid Id { get; set; }
    public ExceptionRuleType RuleType { get; set; }
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdPercent { get; set; }
    public decimal? ThresholdValue { get; set; }
    public ExceptionSeverity Severity { get; set; }
    public bool IsEnabled { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateExceptionRuleRequest
{
    public ExceptionRuleType RuleType { get; set; }
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdPercent { get; set; }
    public decimal? ThresholdValue { get; set; }
    public ExceptionSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateExceptionRuleRequest
{
    public int? ThresholdMinutes { get; set; }
    public decimal? ThresholdPercent { get; set; }
    public decimal? ThresholdValue { get; set; }
    public ExceptionSeverity Severity { get; set; }
    public bool IsEnabled { get; set; }
}
