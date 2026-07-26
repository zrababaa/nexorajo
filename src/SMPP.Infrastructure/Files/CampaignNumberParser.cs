using SMPP.Application.Abstractions;

namespace SMPP.Infrastructure.Files;

/// <summary>
/// Normalizes recipient numbers, improving on legacy's version in one respect: each token is
/// stripped down to digits (optionally keeping a leading '+') before dedup, rather than legacy
/// only trimming stray commas/whitespace and keeping whatever punctuation a user pasted -
/// pasted numbers with spaces or dashes now survive instead of becoming unusable send targets.
/// </summary>
public class CampaignNumberParser : ICampaignNumberParser
{
    public NumberListResult ParsePasted(string rawNumbers)
    {
        var tokens = rawNumbers.Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);
        return BuildResult(tokens);
    }

    public NumberListResult ParseCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        var tokens = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            var firstColumn = line.Split(',')[0];
            tokens.Add(firstColumn);
        }
        return BuildResult(tokens);
    }

    private static NumberListResult BuildResult(IEnumerable<string> rawTokens)
    {
        var normalized = rawTokens
            .Select(NormalizeNumber)
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();

        return new NumberListResult(string.Join(',', normalized), normalized.Count);
    }

    private static string NormalizeNumber(string token)
    {
        var trimmed = token.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? string.Empty : (hasPlus ? "+" + digits : digits);
    }
}
