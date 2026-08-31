namespace SMPP.Application.LinkTracking;

/// <summary>
/// The write path used by SendCore: extracts every http(s) URL in an outgoing message, mints a
/// TrackedLink row for each distinct one, and returns the message with every occurrence swapped
/// for its short tracking-link form. Added rows are not saved here - SendCore's own
/// SaveChangesAsync persists them together with the UnderProcess row it writes.
/// </summary>
public interface ILinkTrackingService
{
    /// <param name="shortenLinks">
    /// When true, each <c>{BaseUrl}/l/{token}</c> tracking link is additionally run through the
    /// configured URL shortener (Short.io) before it goes into the message. A shortening failure
    /// is non-fatal: the full tracking link is used instead so the send still goes out.
    /// </param>
    Task<string> RewriteMessageAsync(
        string message, string batchId, int userId, bool shortenLinks = false, CancellationToken ct = default);

    /// <summary>
    /// Mints a tracking link for <paramref name="destinationUrl"/> now, before any send: saves a
    /// TrackedLink row with an empty batch id (claimed later by the send whose message carries the
    /// link) and returns the link to insert. When <paramref name="shorten"/> is true the
    /// <c>{BaseUrl}/l/{token}</c> redirect is additionally run through Short.io; if that fails the
    /// full-length link is returned instead. Throws <see cref="Common.AppException"/> when the URL
    /// is not an absolute http(s) URL or link tracking is not configured.
    /// </summary>
    Task<PreparedLinkDto> PrepareLinkAsync(
        string destinationUrl, int userId, bool shorten, CancellationToken ct = default);
}
