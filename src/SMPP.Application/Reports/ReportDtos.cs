using SMPP.Application.Common;
using SMPP.Domain.Enums;

namespace SMPP.Application.Reports;

/// <summary>
/// The reports the Reports tab offers. The enum is also the route value and the export key, so
/// adding a report means adding a member here, a case in ReportService, and a view.
/// </summary>
public enum ReportType
{
    /// <summary>Per-recipient message rows - the finest grain available.</summary>
    Messages = 0,

    /// <summary>One row per calendar day: volume and delivery outcome.</summary>
    DailyTraffic = 1,

    /// <summary>One row per send batch: size, outcome, and what it cost.</summary>
    Batches = 2,

    /// <summary>One row per account: volume, delivery rate, credits in and out. Superadmin only.</summary>
    AccountUsage = 3,

    /// <summary>The balance ledger - every credit and debit.</summary>
    Transactions = 4,

    /// <summary>Submitted credit requests and how they were reviewed.</summary>
    CreditRequests = 5,
}

/// <summary>
/// Shared filter for every report. Not every report honors every field - the message-grain
/// reports use <see cref="Source"/>/<see cref="Status"/>, the billing ones ignore them - but
/// one shape keeps the filter bar and the export links identical across tabs.
/// </summary>
public record ReportFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    MessageSource? Source = null,
    MessageStatus? Status = null,
    int? AccountId = null);

public record DailyTrafficRowDto(
    DateOnly Date,
    int Total,
    int Delivered,
    int Sent,
    int Processing,
    int Undelivered,
    int Failed,
    int Expired)
{
    /// <summary>Delivered as a share of everything that has stopped moving; null while nothing has settled.</summary>
    public double? DeliveryRate
    {
        get
        {
            var settled = Total - Processing;
            return settled == 0 ? null : Math.Round(Delivered * 100.0 / settled, 1);
        }
    }
}

public record BatchReportRowDto(
    string BatchId,
    string? CampaignName,
    MessageSource Source,
    string SenderId,
    string AccountUsername,
    int Recipients,
    int Delivered,
    int Failed,
    int Pending,
    decimal Cost,
    DateTime CreatedAt);

public record AccountUsageRowDto(
    int UserId,
    string Username,
    string FullName,
    bool IsActive,
    int TotalSent,
    int Delivered,
    int Failed,
    double? DeliveryRate,
    decimal CreditsConsumed,
    decimal CreditsAdded,
    decimal Balance);

public record TransactionReportRowDto(
    int Id,
    DateTime CreatedAt,
    string Username,
    TransactionKind Kind,
    TransactionSource Source,
    decimal Amount,
    string? RelatedBatchId);

public record CreditRequestRowDto(
    int Id,
    DateTime CreatedAt,
    string Username,
    decimal Amount,
    PaymentMethod Method,
    string? TransactionRef,
    PaymentStatus Status,
    DateTime? ReviewedAt,
    string? ReviewNote);

/// <summary>
/// A report flattened to strings for export. Every report renders to this shape, so CSV and
/// Excel are written once (ReportExportWriter) rather than once per report.
/// </summary>
public record ReportTable(string FileNameStem, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// The footer totals for a report table are computed over every row matching the filter, not
/// just the page on screen - otherwise "Total" would silently mean "total of this page" the
/// moment a report grew past one page.
/// </summary>
public record BatchTotals(int Recipients, int Delivered, int Failed, int Pending, decimal Cost);

public record AccountUsageTotals(int TotalSent, int Delivered, int Failed, decimal CreditsConsumed, decimal CreditsAdded, decimal Balance);

public record DailyTrafficTotals(int Total, int Delivered, int Sent, int Processing, int Undelivered, int Failed, int Expired);

public record TransactionTotals(decimal NetChange);

public record CreditRequestTotals(decimal ApprovedTotal);
