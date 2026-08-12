namespace SMPP.Application.LinkTracking;

/// <summary>
/// The write path used by SendCore: extracts every http(s) URL in an outgoing message, mints a
/// TrackedLink row for each distinct one, and returns the message with every occurrence swapped
/// for its short tracking-link form. Added rows are not saved here - SendCore's own
/// SaveChangesAsync persists them together with the UnderProcess row it writes.
/// </summary>
public interface ILinkTrackingService
{
    Task<string> RewriteMessageAsync(string message, string batchId, int userId, CancellationToken ct = default);
}
