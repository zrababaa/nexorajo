using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

public class UserPackage : AuditableEntity, IHasCreator
{
    public string Name { get; set; } = string.Empty;
    public decimal RateEachMessage { get; set; }
    public int CreatedByUserId { get; set; }
}
