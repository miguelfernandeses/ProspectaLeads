using Microsoft.EntityFrameworkCore;
using ProspeccaoLeads.Domain.Entities;
using ProspeccaoLeads.Domain.Interfaces;
using ProspeccaoLeads.Infrastructure.Data;

namespace ProspeccaoLeads.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task<UserProfile> AddOrUpdateAsync(UserProfile user, CancellationToken ct = default)
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        if (existing == null)
        {
            await _context.Users.AddAsync(user, ct);
        }
        else
        {
            existing.Name = user.Name;
            existing.Email = user.Email;
            _context.Users.Update(existing);
        }
        await _context.SaveChangesAsync(ct);
        return user;
    }
}
