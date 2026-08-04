using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.History;
using SMPP.Application.Reports;
using SMPP.Domain.Common;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Backs the Reports tab. Everything here obeys two rules the rest of the reporting code
/// already lives by:
///
/// - Send figures span both of the daemon's log tables, <c>historys</c> and
///   <c>quick_send_history</c>, because Quick Send rows only ever land in the latter. Counting
///   one would under-report every account that uses Quick Send.
/// - Row sets are capped. A report is something a human reads or exports to a spreadsheet, not
///   a full table dump, and the legacy log tables are large.
/// </summary>
public class ReportService : IReportService
{
    private const int MaxRows = 5_000;
    private const int MaxBatchRows = 2_000;

    /// <summary>MySQL copes badly with a single enormous IN list, so batch-id lookups go in slices.</summary>
    private const int LookupChunkSize = 500;

    private readonly SmppDbContext _db;
    private readonly IUserScopeResolver _scopeResolver;
    private readonly QuickSendHistoryReader _quickSendReader;
    private readonly IHistoryService _historyService;

    public ReportService(
        SmppDbContext db,
        IUserScopeResolver scopeResolver,
        QuickSendHistoryReader quickSendReader,
        IHistoryService historyService)
    {
        _db = db;
        _scopeResolver = scopeResolver;
        _quickSendReader = quickSendReader;
        _historyService = historyService;
    }

    public Task<IReadOnlyList<HistoryExportRowDto>> GetMessagesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default) =>
        _historyService.GetForExportAsync(currentUserId, role, ToHistoryFilter(filter), ct);

    public async Task<IReadOnlyList<DailyTrafficRowDto>> GetDailyTrafficAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        var bulk = await ApplyHistoryFilters(_db.Histories.AsNoTracking(), userIds, filter)
            .GroupBy(h => h.CreatedAt.Date)
            .Select(g => new DayCounts(
                g.Key,
                g.Count(),
                g.Count(x => x.Status == MessageStatus.Delivered),
                g.Count(x => x.Status == MessageStatus.Sent),
                g.Count(x => x.Status == MessageStatus.Processing),
                g.Count(x => x.Status == MessageStatus.Undelivered),
                g.Count(x => x.Status == MessageStatus.Failed),
                g.Count(x => x.Status == MessageStatus.Expired)))
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(userIds, filter, new List<DayCounts>(), query => query
            .GroupBy(h => h.CreatedAt.Date)
            .Select(g => new DayCounts(
                g.Key,
                g.Count(),
                g.Count(x => x.Status == MessageStatus.Delivered),
                g.Count(x => x.Status == MessageStatus.Sent),
                g.Count(x => x.Status == MessageStatus.Processing),
                g.Count(x => x.Status == MessageStatus.Undelivered),
                g.Count(x => x.Status == MessageStatus.Failed),
                g.Count(x => x.Status == MessageStatus.Expired)))
            .ToListAsync(ct));

        return bulk
            .Concat(quick)
            .GroupBy(r => DateOnly.FromDateTime(r.Date))
            .Select(g => new DailyTrafficRowDto(
                g.Key,
                g.Sum(r => r.Total),
                g.Sum(r => r.Delivered),
                g.Sum(r => r.Sent),
                g.Sum(r => r.Processing),
                g.Sum(r => r.Undelivered),
                g.Sum(r => r.Failed),
                g.Sum(r => r.Expired)))
            .OrderByDescending(r => r.Date)
            .Take(MaxRows)
            .ToList();
    }

    public async Task<IReadOnlyList<BatchReportRowDto>> GetBatchesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        // A status filter would silently distort every count on this report (a batch's "failed"
        // column cannot be read off a set already narrowed to failures), so it is not applied.
        var batchFilter = filter with { Status = null };

        // Projected to an anonymous type rather than straight to BatchCounts: "newest batch
        // first" is an ORDER BY over the MIN(created_at) aggregate, and EF can only bind that
        // back to the projection through an anonymous type's members, not through a record's
        // constructor parameters. The record is built once the rows are in memory.
        var bulk = await ApplyHistoryFilters(_db.Histories.AsNoTracking(), userIds, batchFilter)
            .GroupBy(h => new { h.CampaignBatchId, h.CampaignName, h.Source, h.SenderNumber, h.CreatedByUserId })
            .Select(g => new
            {
                g.Key,
                Recipients = g.Count(),
                Delivered = g.Count(x => x.Status == MessageStatus.Delivered),
                Failed = g.Count(x => x.Status == MessageStatus.Failed
                    || x.Status == MessageStatus.Undelivered
                    || x.Status == MessageStatus.Expired),
                Pending = g.Count(x => x.Status == MessageStatus.Processing),
                CreatedAt = g.Min(x => x.CreatedAt),
            })
            .OrderByDescending(g => g.CreatedAt)
            .Take(MaxBatchRows)
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(userIds, batchFilter, new List<BatchCounts>(), async query =>
        {
            var rows = await query
                .GroupBy(h => new { h.CampaignBatchId, h.CampaignName, h.SenderNumber, h.CreatedByUserId })
                .Select(g => new
                {
                    g.Key,
                    Recipients = g.Count(),
                    Delivered = g.Count(x => x.Status == MessageStatus.Delivered),
                    Failed = g.Count(x => x.Status == MessageStatus.Failed
                        || x.Status == MessageStatus.Undelivered
                        || x.Status == MessageStatus.Expired),
                    Pending = g.Count(x => x.Status == MessageStatus.Processing),
                    CreatedAt = g.Min(x => x.CreatedAt),
                })
                .OrderByDescending(g => g.CreatedAt)
                .Take(MaxBatchRows)
                .ToListAsync(ct);

            return rows
                .Select(r => new BatchCounts(
                    r.Key.CampaignBatchId, r.Key.CampaignName, MessageSource.QuickSend, r.Key.SenderNumber,
                    r.Key.CreatedByUserId, r.Recipients, r.Delivered, r.Failed, r.Pending, r.CreatedAt))
                .ToList();
        });

        var batches = bulk
            .Select(r => new BatchCounts(
                r.Key.CampaignBatchId, r.Key.CampaignName, r.Key.Source, r.Key.SenderNumber,
                r.Key.CreatedByUserId, r.Recipients, r.Delivered, r.Failed, r.Pending, r.CreatedAt))
            .Concat(quick)
            .OrderByDescending(b => b.CreatedAt)
            .Take(MaxBatchRows)
            .ToList();

        var usernames = await GetUsernamesAsync(batches.Select(b => b.CreatedByUserId), ct);
        var costs = await GetBatchCostsAsync(batches.Select(b => b.CampaignBatchId), ct);

        return batches
            .Select(b => new BatchReportRowDto(
                b.CampaignBatchId,
                b.CampaignName,
                b.Source,
                b.SenderNumber,
                usernames.GetValueOrDefault(b.CreatedByUserId, b.CreatedByUserId.ToString(CultureInfo.InvariantCulture)),
                b.Recipients,
                b.Delivered,
                b.Failed,
                b.Pending,
                costs.GetValueOrDefault(b.CampaignBatchId),
                b.CreatedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<AccountUsageRowDto>> GetAccountUsageAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        // As on the batch report, a status filter would make the delivered/failed columns
        // meaningless, so only the date range and channel narrow the traffic figures.
        var usageFilter = filter with { Status = null };

        var bulk = await ApplyHistoryFilters(_db.Histories.AsNoTracking(), userIds, usageFilter)
            .GroupBy(h => h.CreatedByUserId)
            .Select(g => new UserCounts(
                g.Key,
                g.Count(),
                g.Count(x => x.Status == MessageStatus.Delivered),
                g.Count(x => x.Status == MessageStatus.Failed
                    || x.Status == MessageStatus.Undelivered
                    || x.Status == MessageStatus.Expired),
                g.Count(x => x.Status == MessageStatus.Processing)))
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(userIds, usageFilter, new List<UserCounts>(), query => query
            .GroupBy(h => h.CreatedByUserId)
            .Select(g => new UserCounts(
                g.Key,
                g.Count(),
                g.Count(x => x.Status == MessageStatus.Delivered),
                g.Count(x => x.Status == MessageStatus.Failed
                    || x.Status == MessageStatus.Undelivered
                    || x.Status == MessageStatus.Expired),
                g.Count(x => x.Status == MessageStatus.Processing)))
            .ToListAsync(ct));

        var traffic = bulk
            .Concat(quick)
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => new UserCounts(
                g.Key, g.Sum(r => r.Total), g.Sum(r => r.Delivered), g.Sum(r => r.Failed), g.Sum(r => r.Processing)));

        var ledger = await ApplyDateRange(
                _db.Transactions.AsNoTracking().Where(t => userIds.Contains(t.UserId)), filter)
            .GroupBy(t => new { t.UserId, t.Kind })
            .Select(g => new LedgerTotal(g.Key.UserId, g.Key.Kind, g.Sum(t => t.Amount)))
            .ToListAsync(ct);

        var consumed = ledger.Where(l => l.Kind == TransactionKind.Debit).ToDictionary(l => l.UserId, l => l.Total);
        var added = ledger.Where(l => l.Kind == TransactionKind.Credit).ToDictionary(l => l.UserId, l => l.Total);

        // Superadmin sends free and holds no customer balance, so it is not a row on a usage
        // report - an Account viewing this only ever sees itself in any case.
        var accounts = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.Role == UserRole.Account)
            .OrderBy(u => u.UserName)
            .Select(u => new { u.Id, Username = u.UserName!, u.FullName, u.IsActive, u.Balance })
            .ToListAsync(ct);

        return accounts
            .Select(a =>
            {
                var t = traffic.GetValueOrDefault(a.Id) ?? new UserCounts(a.Id, 0, 0, 0, 0);
                var settled = t.Total - t.Processing;

                return new AccountUsageRowDto(
                    a.Id,
                    a.Username,
                    a.FullName,
                    a.IsActive,
                    t.Total,
                    t.Delivered,
                    t.Failed,
                    settled == 0 ? null : Math.Round(t.Delivered * 100.0 / settled, 1),
                    consumed.GetValueOrDefault(a.Id),
                    added.GetValueOrDefault(a.Id),
                    a.Balance);
            })
            .OrderByDescending(a => a.TotalSent)
            .ThenBy(a => a.Username)
            .ToList();
    }

    public async Task<IReadOnlyList<TransactionReportRowDto>> GetTransactionsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        var rows = await ApplyDateRange(
                _db.Transactions.AsNoTracking().Where(t => userIds.Contains(t.UserId)), filter)
            .OrderByDescending(t => t.CreatedAt)
            .Take(MaxRows)
            .Select(t => new { t.Id, t.CreatedAt, t.UserId, t.Kind, t.Source, t.Amount, t.RelatedBatchId })
            .ToListAsync(ct);

        var usernames = await GetUsernamesAsync(rows.Select(r => r.UserId), ct);

        return rows
            .Select(r => new TransactionReportRowDto(
                r.Id,
                r.CreatedAt,
                usernames.GetValueOrDefault(r.UserId, r.UserId.ToString(CultureInfo.InvariantCulture)),
                r.Kind,
                r.Source,
                r.Amount,
                r.RelatedBatchId))
            .ToList();
    }

    public async Task<IReadOnlyList<CreditRequestRowDto>> GetCreditRequestsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        var rows = await ApplyDateRange(
                _db.Payments.AsNoTracking().Where(p => userIds.Contains(p.SubmittedByUserId)), filter)
            .OrderByDescending(p => p.CreatedAt)
            .Take(MaxRows)
            .Select(p => new
            {
                p.Id, p.CreatedAt, p.SubmittedByUserId, p.Amount, p.Method,
                p.TransactionRef, p.Status, p.ReviewedAt, p.ReviewNote,
            })
            .ToListAsync(ct);

        var usernames = await GetUsernamesAsync(rows.Select(r => r.SubmittedByUserId), ct);

        return rows
            .Select(r => new CreditRequestRowDto(
                r.Id,
                r.CreatedAt,
                usernames.GetValueOrDefault(r.SubmittedByUserId, r.SubmittedByUserId.ToString(CultureInfo.InvariantCulture)),
                r.Amount,
                r.Method,
                r.TransactionRef,
                r.Status,
                r.ReviewedAt,
                r.ReviewNote))
            .ToList();
    }

    public async Task<ReportTable> BuildExportAsync(
        ReportType type, int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        switch (type)
        {
            case ReportType.DailyTraffic:
            {
                var rows = await GetDailyTrafficAsync(currentUserId, role, filter, ct);
                return new ReportTable(
                    "daily-traffic",
                    new[] { "Date", "Total", "Delivered", "Sent", "Processing", "Undelivered", "Failed", "Expired", "Delivery rate %" },
                    rows.Select(r => (IReadOnlyList<string>)new[]
                    {
                        r.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Num(r.Total), Num(r.Delivered), Num(r.Sent), Num(r.Processing),
                        Num(r.Undelivered), Num(r.Failed), Num(r.Expired), Rate(r.DeliveryRate),
                    }).ToList());
            }

            case ReportType.Batches:
            {
                var rows = await GetBatchesAsync(currentUserId, role, filter, ct);
                return new ReportTable(
                    "batches",
                    new[] { "Batch Id", "Campaign", "Channel", "Sender", "Account", "Recipients", "Delivered", "Failed", "Processing", "Cost", "Date (UTC)" },
                    rows.Select(r => (IReadOnlyList<string>)new[]
                    {
                        r.BatchId, r.CampaignName ?? string.Empty, r.Source.ToString(), r.SenderId, r.AccountUsername,
                        Num(r.Recipients), Num(r.Delivered), Num(r.Failed), Num(r.Pending), Money(r.Cost), Timestamp(r.CreatedAt),
                    }).ToList());
            }

            case ReportType.AccountUsage:
            {
                var rows = await GetAccountUsageAsync(currentUserId, role, filter, ct);
                return new ReportTable(
                    "account-usage",
                    new[] { "Account", "Full name", "Status", "Messages", "Delivered", "Failed", "Delivery rate %", "Credits used", "Credits added", "Balance" },
                    rows.Select(r => (IReadOnlyList<string>)new[]
                    {
                        r.Username, r.FullName, r.IsActive ? "Active" : "Inactive",
                        Num(r.TotalSent), Num(r.Delivered), Num(r.Failed), Rate(r.DeliveryRate),
                        Money(r.CreditsConsumed), Money(r.CreditsAdded), Money(r.Balance),
                    }).ToList());
            }

            case ReportType.Transactions:
            {
                var rows = await GetTransactionsAsync(currentUserId, role, filter, ct);
                return new ReportTable(
                    "transactions",
                    new[] { "Date (UTC)", "Account", "Type", "Source", "Amount", "Batch Id" },
                    rows.Select(r => (IReadOnlyList<string>)new[]
                    {
                        Timestamp(r.CreatedAt), r.Username, r.Kind.ToString(), r.Source.ToString(),
                        Money(r.Amount), r.RelatedBatchId ?? string.Empty,
                    }).ToList());
            }

            case ReportType.CreditRequests:
            {
                var rows = await GetCreditRequestsAsync(currentUserId, role, filter, ct);
                return new ReportTable(
                    "credit-requests",
                    new[] { "Date (UTC)", "Account", "Amount", "Method", "Reference", "Status", "Reviewed (UTC)", "Review note" },
                    rows.Select(r => (IReadOnlyList<string>)new[]
                    {
                        Timestamp(r.CreatedAt), r.Username, Money(r.Amount), r.Method.ToString(),
                        r.TransactionRef ?? string.Empty, r.Status.ToString(),
                        r.ReviewedAt.HasValue ? Timestamp(r.ReviewedAt.Value) : string.Empty,
                        r.ReviewNote ?? string.Empty,
                    }).ToList());
            }

            default:
            {
                var rows = await GetMessagesAsync(currentUserId, role, filter, ct);
                return new ReportTable(
                    "messages",
                    new[] { "Batch Id", "Channel", "Sender", "Receiver", "Status", "Message", "Gateway Message Id", "Date (UTC)" },
                    rows.Select(r => (IReadOnlyList<string>)new[]
                    {
                        r.CampaignBatchId, r.Source.ToString(), r.SenderNumber, r.ReceiverNumber,
                        r.Status.ToString(), r.MessageText ?? string.Empty, r.ExternalMessageId ?? string.Empty,
                        Timestamp(r.CreatedAt),
                    }).ToList());
            }
        }
    }

    private static HistoryFilterDto ToHistoryFilter(ReportFilterDto filter) =>
        new(filter.Source, filter.Status, CampaignBatchId: null, ReceiverSearch: null, filter.DateFrom, filter.DateTo);

    /// <summary>
    /// The rows this user may see, narrowed further when the report is filtered to one account.
    /// An Account cannot widen its own scope this way: the requested id has to already be in it.
    /// </summary>
    private async Task<IReadOnlyCollection<int>> ResolveUserIdsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct)
    {
        var visible = await _scopeResolver.GetVisibleUserIdsAsync(currentUserId, role, ct);

        if (filter.AccountId is not int accountId)
        {
            return visible;
        }

        return visible.Contains(accountId) ? new[] { accountId } : Array.Empty<int>();
    }

    private static IQueryable<History> ApplyHistoryFilters(
        IQueryable<History> source, IReadOnlyCollection<int> userIds, ReportFilterDto filter)
    {
        var query = source.Where(h => userIds.Contains(h.CreatedByUserId));

        if (filter.Source.HasValue)
        {
            query = query.Where(h => h.Source == filter.Source.Value);
        }
        if (filter.Status.HasValue)
        {
            query = query.Where(h => h.Status == filter.Status.Value);
        }

        return ApplyDateRange(query, filter);
    }

    private static IQueryable<QuickSendHistory> ApplyQuickSendFilters(
        IQueryable<QuickSendHistory> source, IReadOnlyCollection<int> userIds, ReportFilterDto filter)
    {
        var query = source.Where(h => userIds.Contains(h.CreatedByUserId));

        if (filter.Status.HasValue)
        {
            query = query.Where(h => h.Status == filter.Status.Value);
        }

        return ApplyDateRange(query, filter);
    }

    /// <summary>
    /// Dates arrive as whole days in the user's terms; <c>To</c> is inclusive of that whole day,
    /// which is why the upper bound is the following midnight, exclusive. Every table a report
    /// reads carries CreatedAt from <see cref="AuditableEntity"/>, so one method covers them all.
    /// </summary>
    private static IQueryable<T> ApplyDateRange<T>(IQueryable<T> query, ReportFilterDto filter)
        where T : AuditableEntity
    {
        if (filter.DateFrom.HasValue)
        {
            var fromUtc = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.CreatedAt >= fromUtc);
        }
        if (filter.DateTo.HasValue)
        {
            var toUtcExclusive = filter.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(e => e.CreatedAt < toUtcExclusive);
        }

        return query;
    }

    /// <summary>
    /// Runs <paramref name="read"/> over the filtered <c>quick_send_history</c> rows, skipping
    /// the table entirely when the channel filter has already ruled Quick Send out.
    /// </summary>
    private Task<T> ReadQuickSendAsync<T>(
        IReadOnlyCollection<int> userIds, ReportFilterDto filter, T empty, Func<IQueryable<QuickSendHistory>, Task<T>> read)
    {
        if (filter.Source.HasValue && filter.Source.Value != MessageSource.QuickSend)
        {
            return Task.FromResult(empty);
        }

        return _quickSendReader.ReadAsync(empty, query => read(ApplyQuickSendFilters(query, userIds, filter)));
    }

    private async Task<Dictionary<int, string>> GetUsernamesAsync(IEnumerable<int> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        return await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName!, ct);
    }

    /// <summary>
    /// What each batch actually cost, read from the ledger rather than recomputed - the debit
    /// row written at submit time is the only authoritative record of the price charged.
    /// </summary>
    private async Task<Dictionary<string, decimal>> GetBatchCostsAsync(IEnumerable<string> batchIds, CancellationToken ct)
    {
        var costs = new Dictionary<string, decimal>();

        foreach (var chunk in batchIds.Distinct().Chunk(LookupChunkSize))
        {
            var rows = await _db.Transactions
                .AsNoTracking()
                .Where(t => t.Kind == TransactionKind.Debit && t.RelatedBatchId != null && chunk.Contains(t.RelatedBatchId))
                .GroupBy(t => t.RelatedBatchId!)
                .Select(g => new { BatchId = g.Key, Total = g.Sum(t => t.Amount) })
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                costs[row.BatchId] = costs.GetValueOrDefault(row.BatchId) + row.Total;
            }
        }

        return costs;
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Money(decimal value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Rate(double? value) =>
        value?.ToString("0.#", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Timestamp(DateTime value) =>
        value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private record DayCounts(
        DateTime Date, int Total, int Delivered, int Sent, int Processing, int Undelivered, int Failed, int Expired);

    private record BatchCounts(
        string CampaignBatchId, string? CampaignName, MessageSource Source, string SenderNumber,
        int CreatedByUserId, int Recipients, int Delivered, int Failed, int Pending, DateTime CreatedAt);

    private record UserCounts(int UserId, int Total, int Delivered, int Failed, int Processing);

    private record LedgerTotal(int UserId, TransactionKind Kind, decimal Total);
}
