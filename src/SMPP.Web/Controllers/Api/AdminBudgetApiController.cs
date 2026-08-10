using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.AdminBudget;
using SMPP.Application.Common;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record SetAdminBudgetApiRequest
{
    [Range(0, double.MaxValue)]
    public decimal NewBalance { get; init; }

    [MaxLength(500)]
    public string? Note { get; init; }
}

public record AdminBudgetBalanceApiResponse(decimal Balance);

/// <summary>
/// The shared pool of credits a Superadmin can hand out to accounts - every account credit draws
/// from this pool. Superadmin only.
/// </summary>
[Authorize(Roles = RoleNames.Superadmin, AuthenticationSchemes = ApiAuth.Schemes)]
[Route("api/v1/admin-budget")]
[Tags("Admin budget")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
public class AdminBudgetApiController : ApiControllerBase
{
    private readonly IAdminBudgetService _adminBudget;

    public AdminBudgetApiController(IAdminBudgetService adminBudget)
    {
        _adminBudget = adminBudget;
    }

    /// <summary>The pool's current balance.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AdminBudgetBalanceApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken ct) =>
        Ok(new AdminBudgetBalanceApiResponse(await _adminBudget.GetBalanceAsync(ct)));

    /// <summary>Paged history of every manual edit and reservation against the pool.</summary>
    [HttpGet("log")]
    [ProducesResponseType(typeof(PagedResult<AdminBudgetLogRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLog([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _adminBudget.GetLogAsync(page, pageSize, ct));

    /// <summary>Manually sets the pool's total. Rejected if the new balance is negative.</summary>
    [HttpPost("balance")]
    [ProducesResponseType(typeof(AdminBudgetBalanceApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetBalance([FromBody] SetAdminBudgetApiRequest request, CancellationToken ct)
    {
        var balance = await _adminBudget.SetBalanceAsync(CurrentUserId, request.NewBalance, request.Note, ct);
        return Ok(new AdminBudgetBalanceApiResponse(balance));
    }
}
