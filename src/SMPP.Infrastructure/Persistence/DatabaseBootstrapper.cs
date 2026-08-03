using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SMPP.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date at startup, picking a path based on what is already there.
///
/// The app shares smpp_bulk_db_new with the legacy Laravel app and the SMPP daemon, which owns
/// <c>historys</c> and <c>under_process</c>. The baseline EF migrations were written against a
/// database EF created from scratch: replayed onto the shared database they rename a fresh
/// <c>Histories</c> table onto the live <c>historys</c>, recreate <c>under_process</c>, and add a
/// foreign key on <c>creater_id</c> that legacy rows cannot satisfy. MySQL does not roll back DDL,
/// so getting this wrong once leaves the daemon's tables in pieces.
///
/// So: if the daemon's tables are present but the baseline is unapplied, create the app-owned
/// tables from an embedded script and stamp the baseline as applied instead of running it. Any
/// migration added later then applies normally through MigrateAsync, on both kinds of database.
/// </summary>
public class DatabaseBootstrapper
{
    /// <summary>
    /// Last of the three migrations the embedded script stands in for. Its presence in the pending
    /// list is what marks a database as not yet carrying the app's own schema.
    /// </summary>
    private const string BaselineMigrationId = "20260803121726_ReplaceOutboundQueueWithUnderProcess";

    private const string ScriptResourceName = "SMPP.Infrastructure.Persistence.Scripts.CreateAppTables.sql";

    private readonly SmppDbContext _db;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(SmppDbContext db, ILogger<DatabaseBootstrapper> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken ct = default)
    {
        var pending = (await _db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            _logger.LogInformation("Database schema is current; no migrations pending.");
            return;
        }

        if (pending.Contains(BaselineMigrationId) && await HasDaemonTablesAsync(ct))
        {
            await GuardAgainstTableNameCollisionAsync(ct);

            _logger.LogWarning(
                "Found the SMPP daemon's tables with no EF migration history. Creating the app-owned " +
                "tables from the embedded script and stamping the baseline as applied, rather than " +
                "running migrations that would rewrite historys/under_process.");

            await RunEmbeddedScriptAsync(ct);

            pending = (await _db.Database.GetPendingMigrationsAsync(ct)).ToList();
            if (pending.Count == 0)
            {
                _logger.LogInformation("App-owned tables created; schema is current.");
                return;
            }
        }

        _logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        await _db.Database.MigrateAsync(ct);
    }

    /// <summary>
    /// True when the daemon's tables are already present, i.e. this is the shared legacy database
    /// rather than an empty one EF is free to build from its migrations.
    /// </summary>
    private async Task<bool> HasDaemonTablesAsync(CancellationToken ct)
    {
        var count = await ScalarAsync(
            """
            SELECT COUNT(*) FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME IN ('under_process', 'historys')
            """, ct);

        return count > 0;
    }

    /// <summary>
    /// The legacy schema has lowercase <c>campaigns</c> and <c>spam_keywords</c>; the app wants
    /// <c>Campaigns</c> and <c>SpamKeywords</c>. Those coexist only where lower_case_table_names=0
    /// (the Linux default). Where it is 1, CREATE TABLE IF NOT EXISTS silently resolves to the
    /// legacy table, creates nothing, and the app then reads legacy rows through a completely
    /// different column set. Refuse to start rather than let that happen.
    ///
    /// Matches only a name-colliding table that is NOT one of ours - ExternalCampaignCode and
    /// KeywordType exist in the app's tables and in neither legacy table - so a restart after a
    /// successful bootstrap does not trip the guard on the tables it just created.
    /// </summary>
    private async Task GuardAgainstTableNameCollisionAsync(CancellationToken ct)
    {
        var caseInsensitiveNames = await ScalarAsync("SELECT @@global.lower_case_table_names", ct) != 0;
        if (!caseInsensitiveNames)
        {
            return;
        }

        var collisions = await ScalarAsync(
            """
            SELECT COUNT(*) FROM information_schema.TABLES t
            WHERE t.TABLE_SCHEMA = DATABASE()
              AND t.TABLE_NAME IN ('campaigns', 'spam_keywords')
              AND NOT EXISTS (
                  SELECT 1 FROM information_schema.COLUMNS c
                  WHERE c.TABLE_SCHEMA = t.TABLE_SCHEMA
                    AND c.TABLE_NAME = t.TABLE_NAME
                    AND c.COLUMN_NAME IN ('ExternalCampaignCode', 'KeywordType')
              )
            """, ct);

        if (collisions > 0)
        {
            throw new InvalidOperationException(
                "Refusing to create the app's tables: this MySQL server has lower_case_table_names " +
                "set to a non-zero value, and the database already holds legacy campaigns/spam_keywords " +
                "tables that the app's Campaigns/SpamKeywords tables would silently resolve to. " +
                "Rename the legacy tables, or move the app to its own database, before starting.");
        }
    }

    private async Task RunEmbeddedScriptAsync(CancellationToken ct)
    {
        foreach (var statement in SplitStatements(await ReadEmbeddedScriptAsync()))
        {
            await _db.Database.ExecuteSqlRawAsync(statement, ct);
        }
    }

    private static async Task<string> ReadEmbeddedScriptAsync()
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ScriptResourceName}' is missing from the assembly.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Splits on semicolons that end a line. ADO.NET sends one statement per command, and the
    /// script is written to suit: no DELIMITER blocks, no semicolons inside a statement body.
    /// </summary>
    private static IEnumerable<string> SplitStatements(string script)
    {
        var current = new List<string>();

        foreach (var line in script.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.TrimStart().StartsWith("--", StringComparison.Ordinal) || trimmed.Trim().Length == 0)
            {
                continue;
            }

            current.Add(trimmed);

            if (trimmed.TrimEnd().EndsWith(';'))
            {
                yield return string.Join('\n', current);
                current.Clear();
            }
        }
    }

    private async Task<long> ScalarAsync(string sql, CancellationToken ct)
    {
        await using var command = _db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            var result = await command.ExecuteScalarAsync(ct);
            return result is null or DBNull ? 0 : Convert.ToInt64(result);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }
}
