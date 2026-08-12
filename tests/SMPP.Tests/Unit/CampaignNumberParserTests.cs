using ClosedXML.Excel;
using SMPP.Infrastructure.Files;
using Xunit;

namespace SMPP.Tests.Unit;

public class CampaignNumberParserTests
{
    private readonly CampaignNumberParser _parser = new();

    [Fact]
    public void ParsePasted_removes_duplicate_numbers()
    {
        var result = _parser.ParsePasted("+97150111,+97150111,+97150111\n97150222");

        Assert.Equal(2, result.Count);
        Assert.Equal("+97150111,97150222", result.NormalizedNumbers);
    }

    [Fact]
    public void ParseCsv_reads_first_column_and_removes_duplicates()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            "97150111,Ali\n97150222,Sara\n97150111,Duplicate\n"));

        var result = _parser.ParseCsv(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal("97150111,97150222", result.NormalizedNumbers);
    }

    [Fact]
    public void ParseXlsx_reads_first_column_and_removes_duplicates()
    {
        using var stream = BuildWorkbook("97150111", "97150222", "97150111", "97150333");

        var result = _parser.ParseXlsx(stream);

        Assert.Equal(3, result.Count);
        Assert.Equal("97150111,97150222,97150333", result.NormalizedNumbers);
    }

    [Fact]
    public void ParseXlsx_ignores_blank_rows()
    {
        using var stream = BuildWorkbook("97150111", "", "97150222");

        var result = _parser.ParseXlsx(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal("97150111,97150222", result.NormalizedNumbers);
    }

    private static MemoryStream BuildWorkbook(params string[] firstColumnValues)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Numbers");

        for (var i = 0; i < firstColumnValues.Length; i++)
        {
            worksheet.Cell(i + 1, 1).Value = firstColumnValues[i];
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
