namespace SMPP.Application.Accounts;

public record AccountListItemDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    decimal Balance,
    decimal RatePerMessage,
    DateTime CreatedAt);

public record AccountDetailDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    string? MobileNo,
    bool IsActive,
    decimal Balance,
    decimal RatePerMessage,
    string? SenderId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? ApiToken,
    string? ApiSecret);

public record CreateAccountRequest(
    string Username,
    string Email,
    string FullName,
    string? MobileNo,
    string Password,
    decimal InitialBalance,
    decimal RatePerMessage,
    string? SenderId);

public record UpdateAccountRequest(
    string FullName,
    string? MobileNo,
    bool IsActive,
    decimal RatePerMessage,
    string? SenderId,
    DateOnly? DateFrom,
    DateOnly? DateTo);
