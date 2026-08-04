namespace SMPP.Application.Abstractions;

/// <summary>
/// What one account may send under, and what it is charged for it.
///
/// <paramref name="AllowFreeSenderId"/> means the account can type its own Sender ID; otherwise
/// it must pick one of <paramref name="AllowedSenderIds"/>. <paramref name="CreditsPerMessagePart"/>
/// is the credit cost of a single message part for a single recipient - the flat
/// MessagePricing price, or zero for Superadmin, which sends free and is never held to a
/// Sender ID list.
/// </summary>
public record SendPolicy(bool AllowFreeSenderId, IReadOnlyList<string> AllowedSenderIds, decimal CreditsPerMessagePart)
{
    public bool HasSenderIdList => AllowedSenderIds.Count > 0;
}

/// <summary>
/// Single source of truth for the Sender ID rules, applied on the send path (SendCore) so the
/// Quick Send screen, the Bulk Send screen, and the public API are all held to the same rule -
/// the send forms also read it to decide between a dropdown and a free-text box, and to price
/// the message the user is typing.
/// </summary>
public interface ISendPolicyService
{
    Task<SendPolicy> GetAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the Sender ID to send under, or throws when the requested one is not permitted.
    /// An empty request falls back to the account's first assigned Sender ID.
    /// </summary>
    Task<string> ResolveSenderIdAsync(int userId, string? requestedSenderId, CancellationToken ct = default);
}
