using System.Text.RegularExpressions;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Pulls http(s) URLs out of free-text message bodies for link-tracking rewriting. There is no
/// general-purpose URL extractor elsewhere in the codebase - SpamKeywordMatcher.ContainsUrl only
/// substring-matches against a configured blocklist, it does not parse URLs out of arbitrary text.
///
/// Deliberately narrow: only URLs that already carry an explicit http/https scheme count, per
/// the confirmed feature scope ("contains an http(s) URL") - bare "www.example.com" or
/// "tel:"/"mailto:" links are left alone.
/// </summary>
public static class UrlExtractor
{
    private static readonly Regex UrlPattern = new(@"https?://\S+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private static readonly char[] TrailingPunctuation = { '.', ',', '!', '?', ';', ':', '\'', '"', ')', ']', '}', '>' };

    /// <summary>
    /// Every distinct URL in <paramref name="message"/>, in first-appearance order. The same URL
    /// repeated more than once yields one entry - a message that says the same link twice should
    /// still aggregate clicks onto a single token, not silently split stats across two.
    /// </summary>
    public static IReadOnlyList<string> ExtractDistinct(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return Array.Empty<string>();
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        foreach (Match match in UrlPattern.Matches(message))
        {
            var url = TrimTrailingPunctuation(match.Value);
            if (url.Length > 0 && seen.Add(url))
            {
                result.Add(url);
            }
        }

        return result;
    }

    /// <summary>
    /// Strips sentence punctuation the regex greedily swallowed because it isn't part of the URL
    /// (e.g. "see https://x.com/a." or "check https://x.com/a, thanks"). A trailing ')' is only
    /// trimmed when the URL has more ')' than '(' - so a balanced Wikipedia-style URL like
    /// ".../wiki/Foo_(bar)" is left intact, while "(see https://x.com/a)" has its wrapping paren
    /// removed.
    /// </summary>
    private static string TrimTrailingPunctuation(string url)
    {
        while (url.Length > 0 && Array.IndexOf(TrailingPunctuation, url[^1]) >= 0)
        {
            if (url[^1] == ')')
            {
                var opens = url.Count(c => c == '(');
                var closes = url.Count(c => c == ')');
                if (opens >= closes)
                {
                    break;
                }
            }

            url = url[..^1];
        }

        return url;
    }
}
