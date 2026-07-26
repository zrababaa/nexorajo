using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

public class Proxy : AuditableEntity, IHasCreator
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public int CreatedByUserId { get; set; }
}
