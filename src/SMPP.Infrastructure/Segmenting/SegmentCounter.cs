using SMPP.Application.Abstractions;

namespace SMPP.Infrastructure.Segmenting;

/// <summary>
/// The single message-segment-counting algorithm for the whole app (Quick Send, Bulk Send,
/// Bulk-via-Template, and the public API all call this - see ISegmentCounter). Mirrored by
/// wwwroot/js/segment-counter.js so the client-side estimate a user sees always matches what
/// they get charged. Legacy had two divergent server-side implementations (160/309/459 vs
/// 160/306/459 for Latin) with no shared client-side source of truth - this replaces both.
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

    public int CountSegments(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return 0;
        }

        var length = message.Length;
        var isUnicode = message.Any(c => c < PrintableAsciiMin || c > PrintableAsciiMax);

        var singleLimit = isUnicode ? ArabicSingle : LatinSingle;
        var multiPartSize = isUnicode ? ArabicFirstOfMulti : LatinFirstOfMulti;

        if (length <= singleLimit)
        {
            return 1;
        }

        return (int)Math.Ceiling(length / (double)multiPartSize);
    }
}
