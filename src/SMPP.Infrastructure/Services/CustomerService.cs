using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.Customers;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly SmppDbContext _db;

    public CustomerService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CustomerListItemDto>> GetPagedAsync(
        int accountUserId, string? search, CustomerStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking().Where(c => c.AccountId == accountUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.CompanyName != null && c.CompanyName.Contains(search)) ||
                (c.Email != null && c.Email.Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        query = query.OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(c.Id, c.Name, c.CompanyName, c.Email, c.Phone, c.Status, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id, int accountUserId, CancellationToken ct = default)
    {
        var customer = await FindOwnedAsync(id, accountUserId, ct);
        return customer is null ? null : ToDetailDto(customer);
    }

    public async Task<int> CreateAsync(int accountUserId, CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            AccountId = accountUserId,
            Name = request.Name,
            CompanyName = request.CompanyName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Notes = request.Notes,
            Status = request.Status,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return customer.Id;
    }

    public async Task UpdateAsync(int id, int accountUserId, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await FindOwnedAsync(id, accountUserId, ct)
            ?? throw new AppException("Customer not found.");

        customer.Name = request.Name;
        customer.CompanyName = request.CompanyName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Address = request.Address;
        customer.Notes = request.Notes;
        customer.Status = request.Status;
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int accountUserId, CancellationToken ct = default)
    {
        var customer = await FindOwnedAsync(id, accountUserId, ct)
            ?? throw new AppException("Customer not found.");

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(ct);
    }

    private Task<Customer?> FindOwnedAsync(int id, int accountUserId, CancellationToken ct) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.AccountId == accountUserId, ct);

    private static CustomerDetailDto ToDetailDto(Customer c) => new(
        c.Id, c.Name, c.CompanyName, c.Email, c.Phone, c.Address, c.Notes, c.Status, c.CreatedAt, c.UpdatedAt);
}
