namespace SMPP.Application.Common;

/// <summary>
/// Raised instead of a plain <see cref="AppException"/> when the content filter rejects a
/// message, so the send screens can put the offending words in front of the user in a dialog
/// rather than as one more line of flash text. Nothing is charged and nothing is queued.
/// </summary>
public class SpamBlockedException : AppException
{
    public SpamBlockedException(IReadOnlyCollection<string> matchedTerms)
        : base($"Message blocked by the content filter: {string.Join(", ", matchedTerms)}")
    {
        MatchedTerms = matchedTerms;
    }

    public IReadOnlyCollection<string> MatchedTerms { get; }
}
