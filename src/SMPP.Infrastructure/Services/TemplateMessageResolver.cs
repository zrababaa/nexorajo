using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.SmsTemplates;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Turns a raw Message, or an SMS Template plus global variable values, into a final message
/// per recipient number. Shared by an immediate Bulk Send (<see cref="BulkSendService"/>) and a
/// scheduled one firing later (<c>ScheduledSendDispatchJob</c>), so a template is always rendered
/// the same way regardless of when it's sent.
///
/// A template's placeholders are split in two: names matching a Customer field
/// (<see cref="CustomerFields"/>) are resolved per recipient by matching the recipient's number
/// to an account Customer by phone (digits-only comparison, since Campaign numbers are stored
/// digits-only but a Customer's phone is free text); every other placeholder must be supplied in
/// <paramref name="variables"/> up front and is the same for every recipient in the send.
/// </summary>
internal static class TemplateMessageResolver
{
    private static readonly HashSet<string> CustomerFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "CompanyName", "Email", "Phone", "Address",
    };

    public static async Task<IReadOnlyDictionary<string, string>> ResolveAsync(
        SmppDbContext db,
        int ownerUserId,
        IReadOnlyCollection<string> numbers,
        string? message,
        int? templateId,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken ct)
    {
        if (templateId is null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new AppException("Either a message or an SMS template must be supplied.");
            }

            return numbers.Distinct().ToDictionary(n => n, _ => message);
        }

        var template = await db.SmsTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == templateId && t.CreatedByUserId == ownerUserId, ct)
            ?? throw new AppException("SMS template not found.");

        return await RenderAsync(db, ownerUserId, numbers, template.Body, variables, ct);
    }

    /// <summary>Validates a template body against a set of global variable values without needing recipient numbers - used when saving a template-based scheduled send.</summary>
    public static void ValidateGlobalVariables(string body, IReadOnlyDictionary<string, string>? variables)
    {
        var missing = MissingGlobalKeys(body, variables);
        if (missing.Count > 0)
        {
            throw new AppException($"Missing a value for placeholder(s): {string.Join(", ", missing.Select(k => $"[{k}]"))}.");
        }
    }

    public static async Task<IReadOnlyDictionary<string, string>> RenderAsync(
        SmppDbContext db,
        int ownerUserId,
        IReadOnlyCollection<string> numbers,
        string templateBody,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken ct)
    {
        ValidateGlobalVariables(templateBody, variables);

        var globalValues = variables is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(variables, StringComparer.OrdinalIgnoreCase);
        var afterGlobals = TemplatePlaceholders.Render(templateBody, globalValues);

        var customers = await db.Customers.AsNoTracking()
            .Where(c => c.AccountId == ownerUserId && c.Phone != null && c.Phone != "")
            .ToListAsync(ct);

        var byDigits = new Dictionary<string, Customer>();
        foreach (var customer in customers)
        {
            var digits = DigitsOnly(customer.Phone!);
            if (digits.Length > 0)
            {
                byDigits.TryAdd(digits, customer);
            }
        }

        var result = new Dictionary<string, string>();
        foreach (var number in numbers.Distinct())
        {
            byDigits.TryGetValue(DigitsOnly(number), out var customer);

            var perRecipientValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = customer?.Name ?? string.Empty,
                ["CompanyName"] = customer?.CompanyName ?? string.Empty,
                ["Email"] = customer?.Email ?? string.Empty,
                ["Phone"] = customer?.Phone ?? number,
                ["Address"] = customer?.Address ?? string.Empty,
            };

            result[number] = TemplatePlaceholders.Render(afterGlobals, perRecipientValues);
        }

        return result;
    }

    private static IReadOnlyList<string> MissingGlobalKeys(string body, IReadOnlyDictionary<string, string>? variables)
    {
        var placeholders = TemplatePlaceholders.Extract(body);
        var globalKeys = placeholders.Where(p => !CustomerFields.Contains(p)).ToList();
        if (globalKeys.Count == 0)
        {
            return [];
        }

        var supplied = variables is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(variables.Keys, StringComparer.OrdinalIgnoreCase);

        return globalKeys.Where(k => !supplied.Contains(k)).ToList();
    }

    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());
}
