using System.Text;
using SMPP.Application.Reports;

namespace SMPP.Web.Services;

/// <summary>
/// Writes any <see cref="ReportTable"/> as CSV or as an HTML table served with an Excel content
/// type - Excel opens an HTML table natively as a workbook, so a real "Export Excel" button
/// costs no extra NuGet dependency. Reports flatten to one shape precisely so this is written
/// once instead of once per report.
/// </summary>
public static class ReportExportWriter
{
    public const string CsvContentType = "text/csv";
    public const string ExcelContentType = "application/vnd.ms-excel";

    public static byte[] WriteCsv(ReportTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', table.Headers.Select(CsvEscape)));

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(',', row.Select(CsvEscape)));
        }

        return Utf8WithBom(sb.ToString());
    }

    public static byte[] WriteExcelHtml(ReportTable table)
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset=\"utf-8\"></head><body><table border=\"1\"><thead><tr>");
        foreach (var header in table.Headers)
        {
            sb.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(header)).Append("</th>");
        }
        sb.Append("</tr></thead><tbody>");

        foreach (var row in table.Rows)
        {
            sb.Append("<tr>");
            foreach (var cell in row)
            {
                // Excel would read a long digit string (a phone number, a batch id) as a number
                // and mangle it, so every cell is pinned to text.
                sb.Append("<td style=\"mso-number-format:'\\@'\">")
                  .Append(System.Net.WebUtility.HtmlEncode(cell))
                  .Append("</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table></body></html>");
        return Utf8WithBom(sb.ToString());
    }

    /// <summary>Timestamped so repeated exports do not overwrite each other in the browser's downloads.</summary>
    public static string FileName(ReportTable table, string extension) =>
        $"{table.FileNameStem}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{extension}";

    /// <summary>
    /// The BOM is what makes Excel open a UTF-8 file as UTF-8 rather than as the system
    /// codepage, which matters for Arabic sender IDs and message text. It has to be written
    /// explicitly: Encoding.GetBytes never emits the preamble, whatever the encoding was
    /// constructed with.
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
