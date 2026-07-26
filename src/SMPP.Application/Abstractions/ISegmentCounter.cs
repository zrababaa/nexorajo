namespace SMPP.Application.Abstractions;

/// <summary>
/// The single message-segment-counting algorithm, used identically by Quick Send, Bulk Send,
/// Bulk-via-Template, and the public API, and mirrored by the client-side JS module so the
/// estimate a user sees always matches what they get charged. Legacy had two divergent
/// implementations (160/309/459 vs 160/306/459 for Latin) with no shared source of truth.
/// </summary>
public interface ISegmentCounter
{
    int CountSegments(string message);
}
