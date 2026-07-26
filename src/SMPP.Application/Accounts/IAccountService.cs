using SMPP.Application.Common;

namespace SMPP.Application.Accounts;

/// <summary>
/// Superadmin-only management of Account users (2-tier model: Superadmin creates and funds
/// Accounts directly). Balance credit/debit goes through IBalanceLedgerService, not here.
/// </summary>
public interface IAccountService
{
    Task<PagedResult<AccountListItemDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);

    Task<AccountDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<int> CreateAsync(int createdByUserId, CreateAccountRequest request, CancellationToken ct = default);

    Task UpdateAsync(int id, UpdateAccountRequest request, CancellationToken ct = default);

    Task RegenerateApiCredentialsAsync(int id, CancellationToken ct = default);
}
