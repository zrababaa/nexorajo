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

    [Fact]
    public void ParseCsv_with_header_row_extracts_columns_as_recipient_variables()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            "Phone,Name,Amount\n97150111,Ali,100\n97150222,Sara,200\n"));

        var result = _parser.ParseCsv(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal("97150111,97150222", result.NormalizedNumbers);
        Assert.Equal(new[] { "Name", "Amount" }, result.Columns);
        Assert.NotNull(result.RowVariablesByNumber);
        Assert.Equal("Ali", result.RowVariablesByNumber!["97150111"]["Name"]);
        Assert.Equal("100", result.RowVariablesByNumber["97150111"]["Amount"]);
        Assert.Equal("Sara", result.RowVariablesByNumber["97150222"]["Name"]);
    }

    [Fact]
    public void ParseCsv_with_header_row_finds_phone_column_by_alias_regardless_of_position()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            "Name,Mobile,Amount\nAli,97150111,100\n"));

        var result = _parser.ParseCsv(stream);

        Assert.Equal(1, result.Count);
        Assert.Equal("97150111", result.NormalizedNumbers);
        Assert.Equal(new[] { "Name", "Amount" }, result.Columns);
        Assert.Equal("Ali", result.RowVariablesByNumber!["97150111"]["Name"]);
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
