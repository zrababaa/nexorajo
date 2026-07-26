using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Accounts;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly SmppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountService(SmppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PagedResult<AccountListItemDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Users
            .Where(u => u.Role == UserRole.Account)
            .OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AccountListItemDto(u.Id, u.UserName!, u.Email!, u.FullName, u.IsActive, u.Balance, u.RatePerMessage, u.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AccountListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<AccountDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Account, ct);
        return u is null ? null : ToDetailDto(u);
    }

    public async Task<int> CreateAsync(int createdByUserId, CreateAccountRequest request, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            MobileNo = request.MobileNo,
            Role = UserRole.Account,
            IsActive = true,
            Balance = request.InitialBalance,
            RatePerMessage = request.RatePerMessage,
            SenderId = request.SenderId,
            CreatedByUserId = createdByUserId,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Account);
        return user.Id;
    }

    public async Task UpdateAsync(int id, UpdateAccountRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Account, ct)
            ?? throw new AppException("Account not found.");

        user.FullName = request.FullName;
        user.MobileNo = request.MobileNo;
        user.IsActive = request.IsActive;
        user.RatePerMessage = request.RatePerMessage;
        user.SenderId = request.SenderId;
        user.DateFrom = request.DateFrom;
        user.DateTo = request.DateTo;

        await _db.SaveChangesAsync(ct);
    }

    public async Task RegenerateApiCredentialsAsync(int id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new AppException("Account not found.");

        user.ApiToken = Guid.NewGuid().ToString("N");
        user.ApiSecret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        await _db.SaveChangesAsync(ct);
    }

    private static AccountDetailDto ToDetailDto(ApplicationUser u) => new(
        u.Id, u.UserName!, u.Email!, u.FullName, u.MobileNo, u.IsActive, u.Balance, u.RatePerMessage,
        u.SenderId, u.DateFrom, u.DateTo, u.ApiToken, u.ApiSecret);
}
