using System.Text.RegularExpressions;

namespace SMPP.Application.SmsTemplates;

/// <summary>
/// The single <c>[Placeholder]</c> token syntax and substitution algorithm for SMS Templates,
/// shared by template CRUD (to report a template's placeholders) and the sending pipeline (to
/// render a template's body). A token that has no matching value is left in the output as-is
/// (e.g. still reading "[Name]") rather than silently blanked, so a missed mapping is visible in
/// the sent text instead of hidden.
/// </summary>
public static partial class TemplatePlaceholders
{
    [GeneratedRegex(@"\[([A-Za-z0-9_]+)\]")]
    private static partial Regex TokenPattern();

    /// <summary>Every distinct placeholder name used in <paramref name="body"/>, in first-seen order.</summary>
    public static IReadOnlyList<string> Extract(string body) =>
        TokenPattern().Matches(body)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Replaces every <c>[Key]</c> token found in <paramref name="values"/> (case-insensitive); unmapped tokens are left untouched.</summary>
    public static string Render(string body, IReadOnlyDictionary<string, string> values)
    {
        if (values.Count == 0)
        {
            return body;
        }

        var lookup = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

        return TokenPattern().Replace(body, m => lookup.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);
    }
}
