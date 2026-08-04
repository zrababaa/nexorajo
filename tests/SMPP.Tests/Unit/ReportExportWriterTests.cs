using System.Text;
using SMPP.Application.Reports;
using SMPP.Web.Services;
using Xunit;

namespace SMPP.Tests.Unit;

public class ReportExportWriterTests
{
    private static readonly ReportTable Table = new(
        "daily-traffic",
        new[] { "Date", "Note" },
        new IReadOnlyList<string>[]
        {
            new[] { "2026-08-01", "plain" },
            new[] { "2026-08-02", "has, comma and \"quotes\"" },
        });

    [Fact]
    public void Csv_escapes_commas_and_quotes()
    {
        var csv = Decode(ReportExportWriter.WriteCsv(Table));

        Assert.Contains("Date,Note", csv);
        Assert.Contains("\"has, comma and \"\"quotes\"\"\"", csv);
    }

    [Fact]
    public void Csv_starts_with_a_utf8_bom_so_excel_reads_arabic_correctly()
    {
        var bytes = ReportExportWriter.WriteCsv(Table);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3));
    }

    [Fact]
    public void Excel_html_encodes_cell_content_rather_than_emitting_raw_markup()
    {
        var html = Decode(ReportExportWriter.WriteExcelHtml(
            Table with { Rows = new IReadOnlyList<string>[] { new[] { "2026-08-01", "<b>x</b>" } } }));

        Assert.Contains("&lt;b&gt;x&lt;/b&gt;", html);
        Assert.DoesNotContain("<b>x</b>", html);
    }

    [Fact]
    public void File_name_uses_the_report_stem_and_the_requested_extension()
    {
        var name = ReportExportWriter.FileName(Table, "xls");

        Assert.StartsWith("daily-traffic-", name);
        Assert.EndsWith(".xls", name);
    }

    private static string Decode(byte[] bytes) => new UTF8Encoding(false).GetString(bytes);
}
