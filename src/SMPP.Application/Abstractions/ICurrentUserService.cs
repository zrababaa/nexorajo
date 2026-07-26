using SMPP.Domain.Enums;

namespace SMPP.Application.Abstractions;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    UserRole Role { get; }
}
