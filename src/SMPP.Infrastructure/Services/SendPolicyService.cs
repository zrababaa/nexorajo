using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Domain.Enums;
using SMPP.Domain.Pricing;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class SendPolicyService : ISendPolicyService
{
    private readonly SmppDbContext _db;

    public SendPolicyService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<SendPolicy> GetAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Role, u.SenderIds, u.AllowFreeSenderId })
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("Account not found.");

        // Superadmin is not an account that sends on a customer's behalf: it is never held to a
        // Sender ID list and is never charged, so it gets the free-text field and no price.
        return user.Role == UserRole.Superadmin
            ? new SendPolicy(AllowFreeSenderId: true, Array.Empty<string>(), CreditsPerMessagePart: 0m)
            : new SendPolicy(user.AllowFreeSenderId, SplitSenderIds(user.SenderIds), MessagePricing.CreditsPerMessagePart);
    }

    public async Task<string> ResolveSenderIdAsync(int userId, string? requestedSenderId, CancellationToken ct = default)
    {
        var policy = await GetAsync(userId, ct);
        var requested = requestedSenderId?.Trim();

        if (string.IsNullOrEmpty(requested))
        {
            return policy.AllowedSenderIds.FirstOrDefault()
                ?? throw new AppException("No Sender ID was provided and this account has none assigned.");
        }

        if (policy.AllowFreeSenderId || policy.AllowedSenderIds.Contains(requested, StringComparer.OrdinalIgnoreCase))
        {
            return requested;
        }

        throw new AppException(policy.HasSenderIdList
            ? $"Sender ID '{requested}' is not assigned to this account. Choose one of: {string.Join(", ", policy.AllowedSenderIds)}."
            : "This account has no Sender ID assigned. Ask an administrator to assign one.");
    }

    /// <summary>Stored the way the admin typed it: comma-separated, possibly with spaces around each entry.</summary>
    private static IReadOnlyList<string> SplitSenderIds(string? senderIds) =>
        string.IsNullOrWhiteSpace(senderIds)
            ? Array.Empty<string>()
            : senderIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
}
