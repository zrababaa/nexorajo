using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record CreateAccountApiRequest
{
    [Required]
    [MaxLength(64)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string FullName { get; init; } = string.Empty;

    public string? MobileNo { get; init; }

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; init; }

    /// <summary>Comma-separated Sender IDs this account may send under.</summary>
    public string? SenderIds { get; init; }

    /// <summary>When true the account may also type a Sender ID of its own.</summary>
    public bool AllowFreeSenderId { get; init; }
}

public record UpdateAccountApiRequest
{
    [Required]
    public string FullName { get; init; } = string.Empty;

    public string? MobileNo { get; init; }

    public bool IsActive { get; init; } = true;

    public string? SenderIds { get; init; }

    public bool AllowFreeSenderId { get; init; }

    /// <summary>Start of the account's validity window; before it, sign-in is refused.</summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>End of the account's validity window; after it, sign-in is refused.</summary>
    public DateOnly? DateTo { get; init; }
}

public record AdjustBalanceApiRequest
{
    [Range(0.0001, double.MaxValue)]
    public decimal Amount { get; init; }
}

public record BalanceApiResponse(int AccountId, decimal Balance);

public record ApiCredentialsApiResponse(int AccountId, string? ApiToken, string? ApiSecret);

/// <summary>
/// Superadmin-only account administration: provision accounts, set their sender-ID policy and
/// validity window, move credits, and rotate the API credentials an account uses to call this
/// API. Balance changes go through the ledger, so every credit and debit lands in the
/// Transactions report.
/// </summary>
[Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
[Route("api/v1/accounts")]
[Tags("Accounts")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
public class AccountsApiController : ApiControllerBase
{
    private readonly IAccountService _accounts;
    private readonly IBalanceLedgerService _ledger;

    public AccountsApiController(IAccountService accounts, IBalanceLedgerService ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    /// <summary>Lists accounts, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 15, CancellationToken ct = default) =>
        Ok(await _accounts.GetPagedAsync(Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>Id + username for every account - the list behind an "account" filter dropdown.</summary>
    [HttpGet("options")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOptions(CancellationToken ct) => Ok(await _accounts.GetOptionsAsync(ct));

    /// <summary>One account, including its balance and API credentials.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AccountDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var account = await _accounts.GetByIdAsync(id, ct);
        return account is null ? NotFound(new ApiErrorResponse("Account not found.")) : Ok(account);
    }

    /// <summary>Creates an account. It starts active, with the balance given as its opening credit.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAccountApiRequest request, CancellationToken ct)
    {
        var id = await _accounts.CreateAsync(CurrentUserId, new CreateAccountRequest(
            request.Username, request.Email, request.FullName, request.MobileNo, request.Password,
            request.InitialBalance, request.SenderIds, request.AllowFreeSenderId), ct);

        var created = await _accounts.GetByIdAsync(id, ct);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    /// <summary>Updates an account's profile, sender-ID policy, and validity window.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AccountDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountApiRequest request, CancellationToken ct)
    {
        await _accounts.UpdateAsync(id, new UpdateAccountRequest(
            request.FullName, request.MobileNo, request.IsActive, request.SenderIds,
            request.AllowFreeSenderId, request.DateFrom, request.DateTo), ct);

        return Ok(await _accounts.GetByIdAsync(id, ct));
    }

    /// <summary>
    /// Issues a fresh API token/secret for the account and returns them. The previous pair stops
    /// working immediately, so any integration using it has to be updated.
    /// </summary>
    [HttpPost("{id:int}/api-credentials")]
    [ProducesResponseType(typeof(ApiCredentialsApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegenerateApiCredentials(int id, CancellationToken ct)
    {
        await _accounts.RegenerateApiCredentialsAsync(id, ct);

        var account = await _accounts.GetByIdAsync(id, ct);
        return account is null
            ? NotFound(new ApiErrorResponse("Account not found."))
            : Ok(new ApiCredentialsApiResponse(id, account.ApiToken, account.ApiSecret));
    }

    /// <summary>Adds credits to an account and writes the matching ledger entry.</summary>
    [HttpPost("{id:int}/credit")]
    [ProducesResponseType(typeof(BalanceApiResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Credit(int id, [FromBody] AdjustBalanceApiRequest request, CancellationToken ct) =>
        AdjustAsync(id, request.Amount, TransactionKind.Credit, ct);

    /// <summary>Removes credits from an account. Rejected if it would take the balance below zero.</summary>
    [HttpPost("{id:int}/debit")]
    [ProducesResponseType(typeof(BalanceApiResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Debit(int id, [FromBody] AdjustBalanceApiRequest request, CancellationToken ct) =>
        AdjustAsync(id, request.Amount, TransactionKind.Debit, ct);

    private async Task<IActionResult> AdjustAsync(int id, decimal amount, TransactionKind kind, CancellationToken ct)
    {
        var balance = await _ledger.ApplyAsync(new BalanceLedgerRequest(
            id, CurrentUserId, amount, kind, TransactionSource.ManualAdjustment), ct);

        return Ok(new BalanceApiResponse(id, balance));
    }
}
