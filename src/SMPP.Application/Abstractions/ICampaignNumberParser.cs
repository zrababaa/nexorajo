namespace SMPP.Application.Abstractions;

/// <summary>
/// <paramref name="RowVariablesByNumber"/>/<paramref name="Columns"/> are populated only when a
/// CSV/XLSX import had a header row (see <see cref="ICampaignNumberParser.ParseCsv"/>/
/// <see cref="ICampaignNumberParser.ParseXlsx"/>) - null for a plain number list, whatever its
/// source.
/// </summary>
public record NumberListResult(
    string NormalizedNumbers,
    int Count,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? RowVariablesByNumber = null,
    IReadOnlyList<string>? Columns = null);

/// <summary>
/// Normalizes recipient numbers for a Campaign (recipient list), whether pasted as free text
/// or uploaded as a CSV/XLSX/TXT file - both end up as one deduplicated, comma-separated
/// digit-only string (legacy's actual create flow only ever stores a flat number list, despite
/// the DB having unused columns that suggested per-recipient names were once supported).
///
/// A CSV/XLSX file whose first row is a real header (its first cell isn't itself a number) also
/// yields the other columns as per-recipient template variables - see <see cref="NumberListResult"/>.
/// </summary>
public interface ICampaignNumberParser
{
    NumberListResult ParsePasted(string rawNumbers);

    NumberListResult ParseCsv(Stream csvStream);

    NumberListResult ParseXlsx(Stream xlsxStream);
}
