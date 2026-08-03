using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// Reads <c>quick_send_history</c> for the screens that report on sending. It exists because
/// that table is outside this app's schema: the SMPP daemon owns it, no migration here creates
/// it, and a deployment could be pointed at a database that predates it. Every read therefore
/// degrades to "no Quick Send rows" instead of failing the page, in one place rather than at
/// each call site.
/// </summary>
public class QuickSendHistoryReader
{
    private readonly SmppDbContext _db;
    private readonly ILogger<QuickSendHistoryReader> _logger;

    public QuickSendHistoryReader(SmppDbContext db, ILogger<QuickSendHistoryReader> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<T> ReadAsync<T>(T empty, Func<IQueryable<QuickSendHistory>, Task<T>> read)
    {
        try
        {
            return await read(_db.QuickSendHistories.AsNoTracking());
        }
        catch (DbException ex)
        {
            _logger.LogWarning(ex, "Could not read quick_send_history; Quick Send rows are omitted from this view.");
            return empty;
        }
    }
}
