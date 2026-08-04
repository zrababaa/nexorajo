namespace SMPP.Domain.Pricing;

/// <summary>
/// The one price in the system. Sending costs a flat credit per SMS part per recipient for
/// every account - there is deliberately no per-account rate, so an admin cannot end up with
/// accounts silently billed at different (or fractional) prices.
///
/// The legacy <c>RatePerMessage</c> column is still on the users table because the SMPP
/// daemon's schema is shared, but nothing reads it any more.
/// </summary>
public static class MessagePricing
{
    /// <summary>Credits deducted for one message part sent to one recipient.</summary>
    public const decimal CreditsPerMessagePart = 1m;

    /// <summary>Total credits for a send of <paramref name="segments"/> parts to <paramref name="recipientCount"/> numbers.</summary>
    public static decimal CostOf(int recipientCount, int segments) =>
        recipientCount * segments * CreditsPerMessagePart;
}
