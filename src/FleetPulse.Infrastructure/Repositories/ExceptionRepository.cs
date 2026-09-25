using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Repositories;

public class ExceptionRepository : IExceptionRepository
{
    private readonly FleetPulseDbContext _db;

    public ExceptionRepository(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task<FleetException?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Exceptions.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<FleetException?> GetOpenByTripAndRuleAsync(Guid tripId, ExceptionRuleType ruleType, CancellationToken cancellationToken = default)
        => await _db.Exceptions
            .Where(e => e.TripId == tripId && e.RuleType == ruleType && e.Status != ExceptionStatus.Resolved)
            .OrderByDescending(e => e.DetectedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FleetException>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _db.Exceptions
            .Include(e => e.Trip).ThenInclude(t => t.Driver).ThenInclude(d => d.User)
            .Include(e => e.Trip).ThenInclude(t => t.Vehicle)
            .Where(e =>
                (e.Status == ExceptionStatus.Active || e.Status == ExceptionStatus.Acknowledged) &&
                e.Trip.Status == TripStatus.InProgress &&
                e.Trip.Vehicle.IsActive)
            .OrderByDescending(e => e.DetectedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(FleetException exception, CancellationToken cancellationToken = default)
    {
        await _db.Exceptions.AddAsync(exception, cancellationToken);
    }

    public Task DeleteAsync(FleetException exception, CancellationToken cancellationToken = default)
    {
        _db.Exceptions.Remove(exception);
        return Task.CompletedTask;
    }
}