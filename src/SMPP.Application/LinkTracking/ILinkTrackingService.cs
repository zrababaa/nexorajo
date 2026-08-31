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
}
