using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly FleetPulseDbContext _db;

    public UserRepository(FleetPulseDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id, bool includeDriver = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsQueryable();
        if (includeDriver)
            query = query.Include(u => u.Driver);
        return await query.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, bool includeDriver = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsQueryable();
        if (includeDriver)
            query = query.Include(u => u.Driver);
        return await query.SingleOrDefaultAsync(u => u.Email.ToLower() == Normalize(email), cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _db.Users.AnyAsync(u => u.Email.ToLower() == Normalize(email), cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(bool includeDriver = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsQueryable();
        if (includeDriver)
            query = query.Include(u => u.Driver);
        return await query.OrderBy(u => u.Email).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _db.Users.AddAsync(user, cancellationToken);
    }

    private static string Normalize(string email) => (email ?? string.Empty).Trim().ToLower();
}