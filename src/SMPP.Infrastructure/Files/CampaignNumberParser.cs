using ClosedXML.Excel;
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
    private static readonly HashSet<string> PhoneColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "phone", "phonenumber", "number", "mobile", "msisdn",
    };

    public NumberListResult ParsePasted(string rawNumbers)
    {
        var tokens = rawNumbers.Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);
        return BuildResult(tokens);
    }

    /// <summary>
    /// A single-column file (or a multi-column one whose first cell is itself a number, i.e. no
    /// header row) is read as a plain number list - only column 1 matters, exactly as before. A
    /// multi-column file whose first row's first cell is text is treated as headered: row 1 names
    /// the columns, the phone column is picked from <see cref="PhoneColumnAliases"/> (falling back
    /// to column 1), and every other column becomes a per-recipient template variable.
    /// </summary>
    public NumberListResult ParseCsv(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        var rows = new List<string[]>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            rows.Add(line.Split(','));
        }
        return BuildFromRows(rows);
    }

    /// <summary>Reads every used row/column on the first worksheet; see <see cref="ParseCsv"/> for the header-detection rule.</summary>
    public NumberListResult ParseXlsx(Stream xlsxStream)
    {
        using var workbook = new XLWorkbook(xlsxStream);
        var worksheet = workbook.Worksheets.First();
        var used = worksheet.RangeUsed();
        if (used is null)
        {
            return BuildResult([]);
        }

        var columnCount = used.ColumnCount();
        var rows = worksheet.RowsUsed()
            .Select(row => Enumerable.Range(1, columnCount).Select(i => row.Cell(i).GetString()).ToArray())
            .ToList();

        return BuildFromRows(rows);
    }

    private static NumberListResult BuildFromRows(IReadOnlyList<string[]> rows)
    {
        if (rows.Count == 0)
        {
            return BuildResult([]);
        }

        var columnCount = rows.Max(r => r.Length);
        var isHeadered = columnCount > 1 && NormalizeNumber(rows[0].ElementAtOrDefault(0) ?? string.Empty).Length == 0;
        if (!isHeadered)
        {
            return BuildResult(rows.Select(r => r.ElementAtOrDefault(0) ?? string.Empty));
        }

        var headers = rows[0].Select(h => h.Trim()).ToArray();
        var phoneColumnIndex = Array.FindIndex(headers, h => PhoneColumnAliases.Contains(h));
        if (phoneColumnIndex < 0)
        {
            phoneColumnIndex = 0;
        }

        var normalizedNumbers = new List<string>();
        var rowVariables = new Dictionary<string, IReadOnlyDictionary<string, string>>();
        var seenColumns = new List<string>();
        for (var h = 0; h < headers.Length; h++)
        {
            if (h != phoneColumnIndex && headers[h].Length > 0 && !seenColumns.Contains(headers[h], StringComparer.OrdinalIgnoreCase))
            {
                seenColumns.Add(headers[h]);
            }
        }

        foreach (var row in rows.Skip(1))
        {
            var number = NormalizeNumber(row.ElementAtOrDefault(phoneColumnIndex) ?? string.Empty);
            if (number.Length == 0 || rowVariables.ContainsKey(number))
            {
                continue;
            }

            normalizedNumbers.Add(number);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var h = 0; h < headers.Length; h++)
            {
                if (h != phoneColumnIndex && headers[h].Length > 0)
                {
                    values[headers[h]] = (row.ElementAtOrDefault(h) ?? string.Empty).Trim();
                }
            }
            rowVariables[number] = values;
        }

        return new NumberListResult(string.Join(',', normalizedNumbers), normalizedNumbers.Count, rowVariables, seenColumns);
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
