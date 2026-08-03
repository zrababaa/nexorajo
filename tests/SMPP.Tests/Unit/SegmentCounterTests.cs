using SMPP.Infrastructure.Segmenting;
using Xunit;

namespace SMPP.Tests.Unit;

public class SegmentCounterTests
{
    private readonly SegmentCounter _counter = new();

    [Fact]
    public void Empty_message_has_zero_segments()
    {
        Assert.Equal(0, _counter.CountSegments(string.Empty));
    }

    [Theory]
    [InlineData(159, 1)]
    [InlineData(160, 1)]
    [InlineData(161, 2)]
    [InlineData(306, 2)]
    [InlineData(307, 3)]
    public void Latin_message_segments_match_expected_thresholds(int length, int expectedSegments)
    {
        var message = new string('a', length);
        Assert.Equal(expectedSegments, _counter.CountSegments(message));
    }

    [Theory]
    [InlineData(69, 1)]
    [InlineData(70, 1)]
    [InlineData(71, 2)]
    [InlineData(134, 2)]
    [InlineData(135, 3)]
    public void Unicode_message_segments_match_expected_thresholds(int length, int expectedSegments)
    {
        var message = new string('ا', length); // Arabic alef
        Assert.Equal(expectedSegments, _counter.CountSegments(message));
    }

    [Fact]
    public void Mixed_script_message_uses_unicode_thresholds()
    {
        var message = new string('a', 100) + "ا";
        Assert.Equal(2, _counter.CountSegments(message));
    }

    [Fact]
    public void Line_breaks_do_not_push_a_latin_message_onto_unicode_thresholds()
    {
        // GSM-7 carries CR/LF/tab, so a plain Latin message with a line break is still one part
        // at 160 characters - it must not be charged as three.
        var message = new string('a', 79) + "\r\n" + new string('a', 79);

        Assert.Equal(1, _counter.CountSegments(message));
    }
}
