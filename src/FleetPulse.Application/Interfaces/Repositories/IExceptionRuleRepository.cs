using FleetPulse.Domain.Entities;

namespace FleetPulse.Application.Interfaces.Repositories;

public interface IExceptionRuleRepository
{
    Task<ExceptionRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExceptionRule>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExceptionRule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ExceptionRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExceptionRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(ExceptionRule rule, CancellationToken cancellationToken = default);
}