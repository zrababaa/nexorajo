using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>
/// A paired WhatsApp device/session. Read-only in this app: real pairing happens in an
/// external system that writes directly into this table; the app only lists/deletes rows.
/// </summary>
public class Instance : AuditableEntity, IHasCreator
{
    public string ExternalInstanceId { get; set; } = string.Empty;
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CallbackUrl { get; set; }
    public int CreatedByUserId { get; set; }
}
