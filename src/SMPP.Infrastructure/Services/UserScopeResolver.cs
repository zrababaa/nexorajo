using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class UserScopeResolver : IUserScopeResolver
{
    private readonly SmppDbContext _db;

    public UserScopeResolver(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<int>> GetVisibleUserIdsAsync(int currentUserId, UserRole role, CancellationToken ct = default)
    {
        if (role == UserRole.Superadmin)
        {
            return await _db.Users.Select(u => u.Id).ToListAsync(ct);
        }

        return new[] { currentUserId };
    }
}
