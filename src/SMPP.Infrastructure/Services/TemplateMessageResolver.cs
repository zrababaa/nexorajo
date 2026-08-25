using System.Text.Json;
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
/// A template's placeholders are split in three, in resolution order: (1) a Campaign imported
/// from a headered file supplies its own per-recipient columns via
/// <see cref="Campaign.RecipientVariablesJson"/>/<see cref="Campaign.ImportedColumnsJson"/>; (2)
/// names matching a Customer field (<see cref="CustomerFields"/>) fall back to a per-recipient
/// lookup by matching the recipient's number to an account Customer by phone (digits-only
/// comparison, since Campaign numbers are stored digits-only but a Customer's phone is free
/// text) - an imported column of the same name overrides this, since it's the more specific
/// source for this particular send; (3) every other placeholder must be supplied in
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
        Campaign campaign,
        string? message,
        int? templateId,
        IReadOnlyDictionary<string, string>? variables,
        CancellationToken ct)
    {
        var numbers = campaign.Numbers.Split(',', StringSplitOptions.RemoveEmptyEntries);

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

        return await RenderAsync(db, ownerUserId, numbers, template.Body, variables, campaign.RecipientVariablesJson, campaign.ImportedColumnsJson, ct);
    }

    /// <summary>Validates a template body against a set of global variable values without needing recipient numbers - used when saving a template-based scheduled send.</summary>
    public static void ValidateGlobalVariables(string body, IReadOnlyDictionary<string, string>? variables, string? importedColumnsJson) =>
        EnsureNoMissingGlobalKeys(body, variables, JsonColumns.Deserialize(importedColumnsJson));

    public static async Task<IReadOnlyDictionary<string, string>> RenderAsync(
        SmppDbContext db,
        int ownerUserId,
        IReadOnlyCollection<string> numbers,
        string templateBody,
        IReadOnlyDictionary<string, string>? variables,
        string? recipientVariablesJson,
        string? importedColumnsJson,
        CancellationToken ct)
    {
        var importedColumns = JsonColumns.Deserialize(importedColumnsJson);
        var recipientVariables = DeserializeRecipientVariables(recipientVariablesJson);

        EnsureNoMissingGlobalKeys(templateBody, variables, importedColumns);

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

            if (recipientVariables is not null && recipientVariables.TryGetValue(number, out var rowValues))
            {
                foreach (var (key, value) in rowValues)
                {
                    perRecipientValues[key] = value;
                }
            }

            result[number] = TemplatePlaceholders.Render(afterGlobals, perRecipientValues);
        }

        return result;
    }

    private static void EnsureNoMissingGlobalKeys(
        string body, IReadOnlyDictionary<string, string>? variables, IReadOnlyList<string>? importedColumns)
    {
        var missing = MissingGlobalKeys(body, variables, importedColumns);
        if (missing.Count > 0)
        {
            throw new AppException($"Missing a value for placeholder(s): {string.Join(", ", missing.Select(k => $"[{k}]"))}.");
        }
    }

    private static IReadOnlyList<string> MissingGlobalKeys(
        string body, IReadOnlyDictionary<string, string>? variables, IReadOnlyList<string>? importedColumns)
    {
        var placeholders = TemplatePlaceholders.Extract(body);
        var resolvedAutomatically = new HashSet<string>(CustomerFields, StringComparer.OrdinalIgnoreCase);
        if (importedColumns is not null)
        {
            resolvedAutomatically.UnionWith(importedColumns);
        }

        var globalKeys = placeholders.Where(p => !resolvedAutomatically.Contains(p)).ToList();
        if (globalKeys.Count == 0)
        {
            return [];
        }

        var supplied = variables is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(variables.Keys, StringComparer.OrdinalIgnoreCase);

        return globalKeys.Where(k => !supplied.Contains(k)).ToList();
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? DeserializeRecipientVariables(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
            !.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<string, string>)kv.Value);

    private static string DigitsOnly(string value) => new(value.Where(char.IsDigit).ToArray());
}
