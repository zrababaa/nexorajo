using SMPP.Domain.Enums;

namespace SMPP.Application.Abstractions;

/// <summary>
/// Resolves which user IDs a given user is allowed to see data for: an Account sees only
/// itself, a Superadmin sees everyone. Shared by Dashboard, History, and Transactions so
/// scoping rules live in exactly one place.
/// </summary>
public interface IUserScopeResolver
{
    Task<IReadOnlyCollection<int>> GetVisibleUserIdsAsync(int currentUserId, UserRole role, CancellationToken ct = default);
}
