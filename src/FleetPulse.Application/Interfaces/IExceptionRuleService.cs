using FleetPulse.Application.DTOs.ExceptionRule;

namespace FleetPulse.Application.Interfaces;

public interface IExceptionRuleService
{
    Task<IReadOnlyList<ExceptionRuleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ExceptionRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExceptionRuleDto?> CreateAsync(CreateExceptionRuleRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ExceptionRuleDto?> UpdateAsync(Guid id, UpdateExceptionRuleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}