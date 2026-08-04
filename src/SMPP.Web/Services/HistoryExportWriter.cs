using System.Globalization;
using System.Text;
using SMPP.Application.History;

namespace SMPP.Web.Services;

/// <summary>
/// Renders History export rows as CSV or as an HTML table served with an Excel content type -
/// Excel opens an HTML table natively as a workbook, so this needs no extra NuGet dependency
/// for a real "Export to Excel" button.
/// </summary>
public static class HistoryExportWriter
{
    private static readonly string[] Headers =
    {
        "Batch Id", "Source", "Sender", "Receiver", "Status", "Message", "Gateway Message Id", "Date (UTC)"
    };

    public static byte[] WriteCsv(IReadOnlyList<HistoryExportRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Headers.Select(CsvEscape)));

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                r.CampaignBatchId,
                r.Source.ToString(),
                r.SenderNumber,
                r.ReceiverNumber,
                r.Status.ToString(),
                r.MessageText ?? string.Empty,
                r.ExternalMessageId ?? string.Empty,
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            }.Select(CsvEscape)));
        }

        return Utf8WithBom(sb.ToString());
    }

    public static byte[] WriteExcelHtml(IReadOnlyList<HistoryExportRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset=\"utf-8\"></head><body><table border=\"1\"><thead><tr>");
        foreach (var h in Headers)
        {
            sb.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(h)).Append("</th>");
        }
        sb.Append("</tr></thead><tbody>");

        foreach (var r in rows)
        {
            sb.Append("<tr>");
            foreach (var value in new[]
            {
                r.CampaignBatchId,
                r.Source.ToString(),
                r.SenderNumber,
                r.ReceiverNumber,
                r.Status.ToString(),
                r.MessageText ?? string.Empty,
                r.ExternalMessageId ?? string.Empty,
                r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            })
            {
                sb.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(value)).Append("</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table></body></html>");
        return Utf8WithBom(sb.ToString());
    }

    /// <summary>
    /// The BOM has to be written explicitly - Encoding.GetBytes never emits the preamble - and
    /// without it Excel opens the file in the system codepage and mangles Arabic message text.
    /// </summary>
    private static byte[] Utf8WithBom(string content) =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
