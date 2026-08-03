using SMPP.Application.Abstractions;

namespace SMPP.Infrastructure.Segmenting;

/// <summary>
/// The single message-segment-counting algorithm for the whole app (Quick Send, Bulk Send,
/// and the public API all call this via ISegmentCounter/SendCore). Legacy had two divergent
/// implementations (160/309/459 vs 160/306/459 for Latin) with no single source of truth -
/// this replaces both.
/// </summary>
public class SegmentCounter : ISegmentCounter
{
    // Arabic/unicode (non-GSM-7) thresholds, mirroring standard multi-part SMS/WhatsApp segmenting.
    private const int ArabicSingle = 70;
    private const int ArabicFirstOfMulti = 67;

    // Latin/GSM-7 thresholds.
    private const int LatinSingle = 160;
    private const int LatinFirstOfMulti = 153;

    // Basic printable ASCII range (space through tilde) is treated as "Latin"; anything
    // outside it (Arabic, other scripts, emoji, etc.) uses the tighter Arabic thresholds.
    private const char PrintableAsciiMin = ' ';
    private const char PrintableAsciiMax = '~';

    // Line breaks and tabs are printable ASCII's neighbours below the range but are carried by
    // GSM-7 all the same. Without this a plain Latin message costs triple the moment the user
    // presses Enter in the message box.
    private static bool IsGsmWhitespace(char c) => c is '\n' or '\r' or '\t';

    public int CountSegments(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return 0;
        }

        var length = message.Length;
        var isUnicode = message.Any(c => (c < PrintableAsciiMin || c > PrintableAsciiMax) && !IsGsmWhitespace(c));

        var singleLimit = isUnicode ? ArabicSingle : LatinSingle;
        var multiPartSize = isUnicode ? ArabicFirstOfMulti : LatinFirstOfMulti;

        if (length <= singleLimit)
        {
            return 1;
        }

        return (int)Math.Ceiling(length / (double)multiPartSize);
    }
}
