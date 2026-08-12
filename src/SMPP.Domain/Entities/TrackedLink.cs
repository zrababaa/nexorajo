using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>
/// One row per distinct URL rewritten inside a send batch's message body. A batch is one
/// literal message shared by every recipient (see UnderProcess), so a message with two
/// different links produces two rows here, sharing one <see cref="BatchId"/>.
///
/// ClickCount/FirstClickedAt/LastClickedAt are maintained running totals, updated at click
/// time (LinkRedirectController) rather than derived from LinkClick on every read - the same
/// "maintained total" shape ApplicationUser.Balance already uses for the same reason: cheap
/// reads, rare writes.
/// </summary>
public class TrackedLink : AuditableEntity, IHasCreator
{
    public string Token { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public int ClickCount { get; set; }
    public DateTime? FirstClickedAt { get; set; }
    public DateTime? LastClickedAt { get; set; }
}
