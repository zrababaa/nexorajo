namespace SMPP.Application.Accounts;

public record AccountOptionDto(int Id, string Username);

public record AccountListItemDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    decimal Balance,
    DateTime CreatedAt);

public record AccountDetailDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    string? MobileNo,
    bool IsActive,
    decimal Balance,
    string? SenderIds,
    bool AllowFreeSenderId,
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
    string? SenderIds,
    bool AllowFreeSenderId);

public record UpdateAccountRequest(
    string FullName,
    string? MobileNo,
    bool IsActive,
    string? SenderIds,
    bool AllowFreeSenderId,
    DateOnly? DateFrom,
    DateOnly? DateTo);
