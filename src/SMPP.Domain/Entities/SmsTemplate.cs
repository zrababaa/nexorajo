using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>
/// A reusable SMS body with <c>[Placeholder]</c> tokens (e.g. "Hello [Name], please come at
/// this [Date]"), scoped to its owning user. <see cref="Name"/>/<see cref="CompanyName"/>/
/// <see cref="Email"/>/<see cref="Phone"/>/<see cref="Address"/>-named placeholders are resolved
/// per recipient from the account's Customers (matched by phone number) when the template is
/// sent against a Campaign; every other placeholder must be supplied as a value at send time.
/// </summary>
public class SmsTemplate : AuditableEntity, IHasCreator
{
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
}
