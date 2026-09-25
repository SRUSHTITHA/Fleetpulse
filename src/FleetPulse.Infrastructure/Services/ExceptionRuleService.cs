using FleetPulse.Application.DTOs.ExceptionRule;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Infrastructure.Services;

public class ExceptionRuleService : IExceptionRuleService
{
    private readonly IExceptionRuleRepository _rules;
    private readonly IUnitOfWork _unitOfWork;

    public ExceptionRuleService(IExceptionRuleRepository rules, IUnitOfWork unitOfWork)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ExceptionRuleDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await _rules.GetAllAsync(cancellationToken)).Select(Map).ToList();

    public async Task<ExceptionRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _rules.GetByIdAsync(id, cancellationToken);
        return rule is null ? null : Map(rule);
    }

    public async Task<ExceptionRuleDto?> CreateAsync(CreateExceptionRuleRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var existing = (await _rules.GetAllAsync(cancellationToken))
            .FirstOrDefault(r => r.RuleType == request.RuleType);

        if (existing is not null)
        {
            Apply(existing, request.ThresholdMinutes, request.ThresholdPercent, request.ThresholdValue, request.Severity, request.IsEnabled);
            await _rules.UpdateAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(existing);
        }

        var rule = new ExceptionRule
        {
            Id = Guid.NewGuid(),
            RuleType = request.RuleType,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        Apply(rule, request.ThresholdMinutes, request.ThresholdPercent, request.ThresholdValue, request.Severity, request.IsEnabled);

        await _rules.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<ExceptionRuleDto?> UpdateAsync(Guid id, UpdateExceptionRuleRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _rules.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return null;

        Apply(existing, request.ThresholdMinutes, request.ThresholdPercent, request.ThresholdValue, request.Severity, request.IsEnabled);

        await _rules.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _rules.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return false;

        await _rules.DeleteAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(ExceptionRule target, int? thresholdMinutes, decimal? thresholdPercent, decimal? thresholdValue, ExceptionSeverity severity, bool isEnabled)
    {
        target.ThresholdMinutes = thresholdMinutes;
        target.ThresholdPercent = thresholdPercent;
        target.ThresholdValue = thresholdValue;
        target.Severity = severity;
        target.IsEnabled = isEnabled;
    }

    private static ExceptionRuleDto Map(ExceptionRule rule) => new()
    {
        Id = rule.Id,
        RuleType = rule.RuleType,
        ThresholdMinutes = rule.ThresholdMinutes,
        ThresholdPercent = rule.ThresholdPercent,
        ThresholdValue = rule.ThresholdValue,
        Severity = rule.Severity,
        IsEnabled = rule.IsEnabled,
        CreatedBy = rule.CreatedBy,
        CreatedAt = rule.CreatedAt,
    };
}