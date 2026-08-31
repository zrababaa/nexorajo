using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

/// <summary>
/// One row per distinct URL rewritten inside a send batch's message body. A batch is one
/// literal message shared by every recipient (see UnderProcess), so a message with two
/// different links produces two rows here, sharing one <see cref="BatchId"/>.
///
/// A row can also be minted ahead of a send, when the composer's "Insert link" button turns a
/// destination URL into its tracking link on the spot. Such a row carries an empty
/// <see cref="BatchId"/> until the send that contains it runs and claims it; one that is never
/// sent just stays unclaimed.
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

    /// <summary>
    /// The Short.io short URL standing in for <c>{BaseUrl}/l/{Token}</c>, set only when the link
    /// was shortened at compose time. Null means the full-length tracking link is what went out.
    /// </summary>
    public string? ShortUrl { get; set; }

    public int CreatedByUserId { get; set; }
    public int ClickCount { get; set; }
    public DateTime? FirstClickedAt { get; set; }
    public DateTime? LastClickedAt { get; set; }
}
