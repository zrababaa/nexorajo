using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Application.Reports;
using SMPP.Domain.Common;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Backs the Reports tab. Everything here obeys the rules the rest of the reporting code
/// already lives by:
///
/// - Send figures span both of the daemon's log tables, <c>historys</c> and
///   <c>quick_send_history</c>, because Quick Send rows only ever land in the latter. Counting
///   one would under-report every account that uses Quick Send.
/// - The on-screen table only ever pulls one page's worth of rows from the database; a report
///   is something a human reads a page at a time, not a full table dump into one HTML response.
///   Export is the deliberate exception (BuildExportAsync / IHistoryService.GetForExportAsync) -
///   a spreadsheet is meant to be a bulk dump, capped generously rather than paged.
/// - Footer "Total" rows are computed over every row matching the filter, not just the page on
///   screen, so they keep meaning "grand total" once a report grows past one page.
/// </summary>
public class ReportService : IReportService
{
    /// <summary>
    /// Batches and daily-traffic rows are grouped/merged across two legacy tables in memory (see
    /// class remarks on HistoryService for why), so - like HistoryService's own list - "all
    /// matching rows" is bounded here rather than a true unbounded scan.
    /// </summary>
    private const int MaxGroupedRows = 2_000;

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

    public async Task<PagedResult<HistoryExportRowDto>> GetMessagesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        // Neither table can contribute more than one page's worth to the merged page, so each
        // side only needs the newest page*pageSize rows before the merge decides the order (same
        // approach as HistoryService.GetPagedAsync for the identical two-table shape).
        var take = page * pageSize;

        var bulkQuery = ApplyHistoryFilters(_db.Histories.AsNoTracking(), userIds, filter);
        var bulk = await bulkQuery
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .Select(h => new HistoryExportRowDto(
                h.Id, h.CampaignBatchId, h.Source, h.SenderNumber, h.ReceiverNumber,
                h.MessageText, h.Status, h.ExternalMessageId, h.CreatedAt))
            .ToListAsync(ct);

        var quick = await ReadQuickSendAsync(userIds, filter, new List<HistoryExportRowDto>(), query => query
            .OrderByDescending(h => h.CreatedAt)
            .Take(take)
            .Select(h => new HistoryExportRowDto(
                h.Id, h.CampaignBatchId, MessageSource.QuickSend, h.SenderNumber, h.ReceiverNumber,
                h.MessageText, h.Status, h.ExternalMessageId, h.CreatedAt))
            .ToListAsync(ct));

        var totalCount = await bulkQuery.CountAsync(ct)
            + await ReadQuickSendAsync(userIds, filter, 0, query => query.CountAsync(ct));

        var items = bulk
            .Concat(quick)
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<HistoryExportRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<(PagedResult<DailyTrafficRowDto> Page, DailyTrafficTotals Totals)> GetDailyTrafficAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);
        var all = await GetDailyTrafficAllAsync(userIds, filter, ct);

        var totals = new DailyTrafficTotals(
            all.Sum(r => r.Total), all.Sum(r => r.Delivered), all.Sum(r => r.Sent), all.Sum(r => r.Processing),
            all.Sum(r => r.Undelivered), all.Sum(r => r.Failed), all.Sum(r => r.Expired));

        return (Paginate(all, page, pageSize), totals);
    }

    private async Task<List<DailyTrafficRowDto>> GetDailyTrafficAllAsync(
        IReadOnlyCollection<int> userIds, ReportFilterDto filter, CancellationToken ct)
    {
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
            .ToList();
    }

    public async Task<(PagedResult<BatchReportRowDto> Page, BatchTotals Totals)> GetBatchesAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);
        var batches = await GetBatchesAllAsync(userIds, filter, ct);

        var totals = new BatchTotals(
            batches.Sum(b => b.Recipients), batches.Sum(b => b.Delivered), batches.Sum(b => b.Failed),
            batches.Sum(b => b.Pending), 0m); // Cost totalled below once costs are looked up.

        var pageOfBatches = batches
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Usernames/costs are only looked up for the page actually shown, not every batch in the
        // (already capped) full match set.
        var usernames = await GetUsernamesAsync(pageOfBatches.Select(b => b.CreatedByUserId), ct);
        var allCosts = await GetBatchCostsAsync(batches.Select(b => b.CampaignBatchId), ct);

        var items = pageOfBatches
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
                allCosts.GetValueOrDefault(b.CampaignBatchId),
                b.CreatedAt))
            .ToList();

        var result = new PagedResult<BatchReportRowDto>
        {
            Items = items,
            TotalCount = batches.Count,
            PageNumber = page,
            PageSize = pageSize,
        };

        return (result, totals with { Cost = batches.Sum(b => allCosts.GetValueOrDefault(b.CampaignBatchId)) });
    }

    private async Task<List<BatchCounts>> GetBatchesAllAsync(
        IReadOnlyCollection<int> userIds, ReportFilterDto filter, CancellationToken ct)
    {
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
            .Take(MaxGroupedRows)
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
                .Take(MaxGroupedRows)
                .ToListAsync(ct);

            return rows
                .Select(r => new BatchCounts(
                    r.Key.CampaignBatchId, r.Key.CampaignName, MessageSource.QuickSend, r.Key.SenderNumber,
                    r.Key.CreatedByUserId, r.Recipients, r.Delivered, r.Failed, r.Pending, r.CreatedAt))
                .ToList();
        });

        return bulk
            .Select(r => new BatchCounts(
                r.Key.CampaignBatchId, r.Key.CampaignName, r.Key.Source, r.Key.SenderNumber,
                r.Key.CreatedByUserId, r.Recipients, r.Delivered, r.Failed, r.Pending, r.CreatedAt))
            .Concat(quick)
            .OrderByDescending(b => b.CreatedAt)
            .Take(MaxGroupedRows)
            .ToList();
    }

    public async Task<(PagedResult<AccountUsageRowDto> Page, AccountUsageTotals Totals)> GetAccountUsageAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
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

        var all = accounts
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

        var totals = new AccountUsageTotals(
            all.Sum(a => a.TotalSent), all.Sum(a => a.Delivered), all.Sum(a => a.Failed),
            all.Sum(a => a.CreditsConsumed), all.Sum(a => a.CreditsAdded), all.Sum(a => a.Balance));

        return (Paginate(all, page, pageSize), totals);
    }

    public async Task<(PagedResult<TransactionReportRowDto> Page, TransactionTotals Totals)> GetTransactionsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);
        var query = ApplyDateRange(_db.Transactions.AsNoTracking().Where(t => userIds.Contains(t.UserId)), filter);

        var totalCount = await query.CountAsync(ct);
        var creditTotal = await query.Where(t => t.Kind == TransactionKind.Credit).SumAsync(t => t.Amount, ct);
        var debitTotal = await query.Where(t => t.Kind == TransactionKind.Debit).SumAsync(t => t.Amount, ct);

        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new { t.Id, t.CreatedAt, t.UserId, t.Kind, t.Source, t.Amount, t.RelatedBatchId })
            .ToListAsync(ct);

        var usernames = await GetUsernamesAsync(rows.Select(r => r.UserId), ct);

        var items = rows
            .Select(r => new TransactionReportRowDto(
                r.Id,
                r.CreatedAt,
                usernames.GetValueOrDefault(r.UserId, r.UserId.ToString(CultureInfo.InvariantCulture)),
                r.Kind,
                r.Source,
                r.Amount,
                r.RelatedBatchId))
            .ToList();

        var result = new PagedResult<TransactionReportRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };

        return (result, new TransactionTotals(creditTotal - debitTotal));
    }

    public async Task<(PagedResult<CreditRequestRowDto> Page, CreditRequestTotals Totals)> GetCreditRequestsAsync(
        int currentUserId, UserRole role, ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);
        var query = ApplyDateRange(_db.Payments.AsNoTracking().Where(p => userIds.Contains(p.SubmittedByUserId)), filter);

        var totalCount = await query.CountAsync(ct);
        var approvedTotal = await query.Where(p => p.Status == PaymentStatus.Approved).SumAsync(p => p.Amount, ct);

        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.CreatedAt, p.SubmittedByUserId, p.Amount, p.Method,
                p.TransactionRef, p.Status, p.ReviewedAt, p.ReviewNote,
            })
            .ToListAsync(ct);

        var usernames = await GetUsernamesAsync(rows.Select(r => r.SubmittedByUserId), ct);

        var items = rows
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

        var result = new PagedResult<CreditRequestRowDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };

        return (result, new CreditRequestTotals(approvedTotal));
    }

    public async Task<ReportTable> BuildExportAsync(
        ReportType type, int currentUserId, UserRole role, ReportFilterDto filter, CancellationToken ct = default)
    {
        var userIds = await ResolveUserIdsAsync(currentUserId, role, filter, ct);

        switch (type)
        {
            case ReportType.DailyTraffic:
            {
                var rows = await GetDailyTrafficAllAsync(userIds, filter, ct);
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
                var (page, _) = await GetBatchesAsync(currentUserId, role, filter, 1, MaxGroupedRows, ct);
                return new ReportTable(
                    "batches",
                    new[] { "Batch Id", "Campaign", "Channel", "Sender", "Account", "Recipients", "Delivered", "Failed", "Processing", "Cost", "Date (UTC)" },
                    page.Items.Select(r => (IReadOnlyList<string>)new[]
                    {
                        r.BatchId, r.CampaignName ?? string.Empty, r.Source.ToString(), r.SenderId, r.AccountUsername,
                        Num(r.Recipients), Num(r.Delivered), Num(r.Failed), Num(r.Pending), Money(r.Cost), Timestamp(r.CreatedAt),
                    }).ToList());
            }

            case ReportType.AccountUsage:
            {
                var (page, _) = await GetAccountUsageAsync(currentUserId, role, filter, 1, int.MaxValue, ct);
                return new ReportTable(
                    "account-usage",
                    new[] { "Account", "Full name", "Status", "Messages", "Delivered", "Failed", "Delivery rate %", "Credits used", "Credits added", "Balance" },
                    page.Items.Select(r => (IReadOnlyList<string>)new[]
                    {
                        r.Username, r.FullName, r.IsActive ? "Active" : "Inactive",
                        Num(r.TotalSent), Num(r.Delivered), Num(r.Failed), Rate(r.DeliveryRate),
                        Money(r.CreditsConsumed), Money(r.CreditsAdded), Money(r.Balance),
                    }).ToList());
            }

            case ReportType.Transactions:
            {
                var (page, _) = await GetTransactionsAsync(currentUserId, role, filter, 1, HistoryService.MaxExportRows, ct);
                return new ReportTable(
                    "transactions",
                    new[] { "Date (UTC)", "Account", "Type", "Source", "Amount", "Batch Id" },
                    page.Items.Select(r => (IReadOnlyList<string>)new[]
                    {
                        Timestamp(r.CreatedAt), r.Username, r.Kind.ToString(), r.Source.ToString(),
                        Money(r.Amount), r.RelatedBatchId ?? string.Empty,
                    }).ToList());
            }

            case ReportType.CreditRequests:
            {
                var (page, _) = await GetCreditRequestsAsync(currentUserId, role, filter, 1, HistoryService.MaxExportRows, ct);
                return new ReportTable(
                    "credit-requests",
                    new[] { "Date (UTC)", "Account", "Amount", "Method", "Reference", "Status", "Reviewed (UTC)", "Review note" },
                    page.Items.Select(r => (IReadOnlyList<string>)new[]
                    {
                        Timestamp(r.CreatedAt), r.Username, Money(r.Amount), r.Method.ToString(),
                        r.TransactionRef ?? string.Empty, r.Status.ToString(),
                        r.ReviewedAt.HasValue ? Timestamp(r.ReviewedAt.Value) : string.Empty,
                        r.ReviewNote ?? string.Empty,
                    }).ToList());
            }

            default:
            {
                var rows = await _historyService.GetForExportAsync(currentUserId, role, ToHistoryFilter(filter), ct);
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

    private static PagedResult<T> Paginate<T>(IReadOnlyList<T> all, int page, int pageSize) => new()
    {
        Items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
        TotalCount = all.Count,
        PageNumber = page,
        PageSize = pageSize,
    };

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
