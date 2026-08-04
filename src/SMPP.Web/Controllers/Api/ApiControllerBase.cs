using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Abstractions;
using SMPP.Domain.Enums;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

/// <summary>
/// Shared setup for the versioned REST API: JSON in and out, bearer-or-cookie authentication,
/// and the documented error shape for the two failures every endpoint can produce.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = ApiAuth.Schemes)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
public abstract class ApiControllerBase : ControllerBase
{
    private ICurrentUserService? _currentUser;

    protected ICurrentUserService CurrentUser =>
        _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    protected int CurrentUserId => CurrentUser.UserId;

    protected UserRole CurrentRole => CurrentUser.Role;

    protected bool IsSuperadmin => CurrentRole == UserRole.Superadmin;
}
