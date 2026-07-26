namespace SMPP.Application.Abstractions;

/// <summary>
/// Loads the current Include/Exclude/Url spam keyword lists and runs the gateway's preflight
/// filter against a message. Used uniformly by Quick Send, Bulk Send, and the public API.
/// </summary>
public interface ISpamKeywordFilterService
{
    Task<SpamFilterResult> CheckAsync(string message, CancellationToken ct = default);
}
