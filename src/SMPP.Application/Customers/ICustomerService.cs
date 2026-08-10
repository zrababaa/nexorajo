using SMPP.Application.Common;
using SMPP.Domain.Enums;

namespace SMPP.Application.Customers;

/// <summary>An Account's own CRM: customer contacts it manages, always scoped to that Account.</summary>
public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        int accountUserId, string? search, CustomerStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<CustomerDetailDto?> GetByIdAsync(int id, int accountUserId, CancellationToken ct = default);

    Task<int> CreateAsync(int accountUserId, CreateCustomerRequest request, CancellationToken ct = default);

    Task UpdateAsync(int id, int accountUserId, UpdateCustomerRequest request, CancellationToken ct = default);

    Task DeleteAsync(int id, int accountUserId, CancellationToken ct = default);
}
