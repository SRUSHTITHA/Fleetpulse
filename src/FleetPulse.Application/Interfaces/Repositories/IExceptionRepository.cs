using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.Interfaces.Repositories;

public interface IExceptionRepository
{
    Task<FleetException?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FleetException?> GetOpenByTripAndRuleAsync(Guid tripId, ExceptionRuleType ruleType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FleetException>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(FleetException exception, CancellationToken cancellationToken = default);
    Task DeleteAsync(FleetException exception, CancellationToken cancellationToken = default);
}