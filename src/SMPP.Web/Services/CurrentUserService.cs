using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SMPP.Application.Abstractions;
using SMPP.Domain.Enums;

namespace SMPP.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public UserRole Role
    {
        get
        {
            if (User?.IsInRole(Infrastructure.Identity.RoleNames.Superadmin) == true)
            {
                return UserRole.Superadmin;
            }
            return UserRole.Account;
        }
    }
}
