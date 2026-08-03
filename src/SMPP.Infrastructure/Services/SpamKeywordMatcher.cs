using System.Text.RegularExpressions;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// The matching half of the content filter, kept free of the database so the rules can be
/// exercised directly. <see cref="SpamKeywordFilterService"/> loads the keyword lists and hands
/// them here.
/// </summary>
public static class SpamKeywordMatcher
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Word-boundary match, case-insensitive, so "cash" does not fire on "cashew". Arabic letters
    /// count as word characters, so the rule holds for both scripts.
    ///
    /// The boundary is only asserted on a side where the keyword itself ends in a word character.
    /// A keyword like "%off" starts with punctuation, and demanding a boundary before it would
    /// mean it never matched inside "50%off" - which is exactly where an operator expects it to.
    /// </summary>
    public static bool ContainsKeyword(string message, string keyword)
    {
        var trimmed = keyword.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        var openBoundary = IsWordCharacter(trimmed[0]) ? @"(?<!\w)" : string.Empty;
        var closeBoundary = IsWordCharacter(trimmed[^1]) ? @"(?!\w)" : string.Empty;

        var pattern = openBoundary + Regex.Escape(trimmed) + closeBoundary;
        return Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>
    /// Plain substring match: a blocked host like "bit.ly" is not word-delimited, and it should
    /// still be caught inside a full URL.
    /// </summary>
    public static bool ContainsUrl(string message, string url)
    {
        var trimmed = url.Trim();
        return trimmed.Length > 0 && message.Contains(trimmed, StringComparison.OrdinalIgnoreCase);
    }
}
