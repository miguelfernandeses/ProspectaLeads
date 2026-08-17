using ProspeccaoLeads.Domain.Entities;

namespace ProspeccaoLeads.Domain.Interfaces;

public interface IUserRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserProfile> AddOrUpdateAsync(UserProfile user, CancellationToken ct = default);
}
