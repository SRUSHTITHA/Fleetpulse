using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Infrastructure.Data;

namespace FleetPulse.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly FleetPulseDbContext _db;

    public UnitOfWork(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}