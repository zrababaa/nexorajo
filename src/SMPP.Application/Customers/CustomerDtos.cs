using SMPP.Domain.Enums;

namespace SMPP.Application.Customers;

public record CustomerListItemDto(
    int Id, string Name, string? CompanyName, string? Email, string? Phone, CustomerStatus Status, DateTime CreatedAt);

public record CustomerDetailDto(
    int Id,
    string Name,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    CustomerStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateCustomerRequest(
    string Name, string? CompanyName, string? Email, string? Phone, string? Address, string? Notes, CustomerStatus Status);

public record UpdateCustomerRequest(
    string Name, string? CompanyName, string? Email, string? Phone, string? Address, string? Notes, CustomerStatus Status);
