using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMPP.Application.Common;
using SMPP.Application.Customers;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Web.Api;

namespace SMPP.Web.Controllers.Api;

public record CreateCustomerApiRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; init; }

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(50)]
    public string? Phone { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    public string? Notes { get; init; }

    public CustomerStatus Status { get; init; } = CustomerStatus.Lead;
}

public record UpdateCustomerApiRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; init; }

    [MaxLength(200)]
    public string? Email { get; init; }

    [MaxLength(50)]
    public string? Phone { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    public string? Notes { get; init; }

    public CustomerStatus Status { get; init; }
}

/// <summary>
/// An Account's own CRM contacts. Every endpoint here is scoped to the calling account - you
/// only ever see, change, or delete your own customers.
/// </summary>
[Route("api/v1/customers")]
[Tags("Customers")]
[Authorize(Roles = RoleNames.Account, AuthenticationSchemes = ApiAuth.Schemes)]
public class CustomersApiController : ApiControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersApiController(ICustomerService customers)
    {
        _customers = customers;
    }

    /// <summary>Lists your customers, optionally filtered by search text and/or status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] CustomerStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _customers.GetPagedAsync(CurrentUserId, search, status, Paging.Page(page), Paging.Size(pageSize), ct));

    /// <summary>One customer's full details.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(id, CurrentUserId, ct);
        return customer is null ? NotFound(new ApiErrorResponse("Customer not found.")) : Ok(customer);
    }

    /// <summary>Adds a new customer.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerApiRequest request, CancellationToken ct)
    {
        var id = await _customers.CreateAsync(CurrentUserId, new CreateCustomerRequest(
            request.Name, request.CompanyName, request.Email, request.Phone, request.Address, request.Notes, request.Status), ct);

        return CreatedAtAction(nameof(GetById), new { id }, await _customers.GetByIdAsync(id, CurrentUserId, ct));
    }

    /// <summary>Updates a customer's details.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerApiRequest request, CancellationToken ct)
    {
        await _customers.UpdateAsync(id, CurrentUserId, new UpdateCustomerRequest(
            request.Name, request.CompanyName, request.Email, request.Phone, request.Address, request.Notes, request.Status), ct);

        return Ok(await _customers.GetByIdAsync(id, CurrentUserId, ct));
    }

    /// <summary>Deletes a customer.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _customers.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }
}
