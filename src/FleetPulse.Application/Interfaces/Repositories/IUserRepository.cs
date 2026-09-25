using FleetPulse.Domain.Entities;

namespace FleetPulse.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, bool includeDriver = false, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, bool includeDriver = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(bool includeDriver = false, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}