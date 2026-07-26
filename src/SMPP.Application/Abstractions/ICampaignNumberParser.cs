namespace SMPP.Application.Abstractions;

public record NumberListResult(string NormalizedNumbers, int Count);

/// <summary>
/// Normalizes recipient numbers for a Campaign (recipient list), whether pasted as free text
/// or uploaded as a CSV/XLSX/TXT file - both end up as one deduplicated, comma-separated
/// digit-only string (legacy's actual create flow only ever stores a flat number list, despite
/// the DB having unused columns that suggested per-recipient names were once supported).
/// </summary>
public interface ICampaignNumberParser
{
    NumberListResult ParsePasted(string rawNumbers);

    NumberListResult ParseCsv(Stream csvStream);
}
