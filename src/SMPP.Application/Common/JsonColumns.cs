using System.Text.Json;

namespace SMPP.Application.Common;

/// <summary>
/// Deserializes the JSON string array stored in <c>Campaign.ImportedColumnsJson</c> - shared by
/// the Campaigns read path (to surface the column names to the API/UI) and
/// <c>TemplateMessageResolver</c> (to know which template placeholders are already resolved
/// per-recipient from an imported file, rather than requiring a global value).
/// </summary>
public static class JsonColumns
{
    public static IReadOnlyList<string>? Deserialize(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<List<string>>(json);
}
