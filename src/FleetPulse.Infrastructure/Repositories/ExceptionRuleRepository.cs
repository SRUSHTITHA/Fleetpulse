using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Repositories;

public class ExceptionRuleRepository : IExceptionRuleRepository
{
    private readonly FleetPulseDbContext _db;

    public ExceptionRuleRepository(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task<ExceptionRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.ExceptionRules.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ExceptionRule>> GetEnabledAsync(CancellationToken cancellationToken = default)
        => await _db.ExceptionRules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.RuleType)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExceptionRule>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.ExceptionRules
            .OrderBy(r => r.RuleType)
            .ThenBy(r => r.Severity)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ExceptionRule rule, CancellationToken cancellationToken = default)
    {
        await _db.ExceptionRules.AddAsync(rule, cancellationToken);
    }

    public Task UpdateAsync(ExceptionRule rule, CancellationToken cancellationToken = default)
    {
        _db.ExceptionRules.Update(rule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExceptionRule rule, CancellationToken cancellationToken = default)
    {
        _db.ExceptionRules.Remove(rule);
        return Task.CompletedTask;
    }
}