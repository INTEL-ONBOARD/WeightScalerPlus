using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;

namespace WeightMaster.Services.Sync
{
    public sealed class TriggerInstallResult
    {
        public int Created { get; set; }
        public int AlreadyCurrent { get; set; }
        public int Replaced { get; set; }
        public int SkippedTables { get; set; }
        public int Failed { get; set; }

        public override string ToString() =>
            $"created={Created} current={AlreadyCurrent} replaced={Replaced} " +
            $"skippedTables={SkippedTables} failed={Failed}";
    }

    /// <summary>
    /// Installs the change-capture triggers on the operational tables.
    ///
    /// Every trigger body opens with a CONTINUE HANDLER for SQLEXCEPTION, so a
    /// failure inside the trigger is swallowed and the application's own write
    /// still commits. That is the whole reason database triggers are acceptable
    /// in a system where a blocked INSERT means a farmer waiting at the scale.
    /// What is lost when a trigger fails is a notification, and the reconciler
    /// recovers exactly that.
    /// </summary>
    public static class TriggerInstaller
    {
        private const string Source = "SyncTriggers";

        /// <summary>
        /// Bumped whenever the trigger body changes. Installed triggers carrying
        /// an older marker are replaced; identical ones are left alone.
        /// </summary>
        private const string BodyVersion = "wm_sync_v1";

        public static async Task<TriggerInstallResult> EnsureAsync(CancellationToken ct = default)
        {
            var result = new TriggerInstallResult();

            try
            {
                using var db = new AppDbContext();
                DbConnection conn = db.Database.GetDbConnection();

                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct).ConfigureAwait(false);

                foreach (string logicalName in SyncTables.Operational)
                {
                    try
                    {
                        string? table = await ResolveTableNameAsync(conn, logicalName, ct)
                            .ConfigureAwait(false);

                        if (table == null)
                        {
                            result.SkippedTables++;
                            Logger.Warn(Source, $"table not found, skipping triggers: {logicalName}");
                            continue;
                        }

                        string? pk = await ResolvePrimaryKeyAsync(conn, table, ct).ConfigureAwait(false);

                        if (pk == null)
                        {
                            result.SkippedTables++;
                            Logger.Warn(Source, $"no single-column primary key, skipping: {table}");
                            continue;
                        }

                        await EnsureTriggerAsync(conn, table, pk, "ai", "INSERT", "NEW", "I", result, ct)
                            .ConfigureAwait(false);
                        await EnsureTriggerAsync(conn, table, pk, "au", "UPDATE", "NEW", "U", result, ct)
                            .ConfigureAwait(false);
                        await EnsureTriggerAsync(conn, table, pk, "ad", "DELETE", "OLD", "D", result, ct)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        Logger.Error(Source, $"trigger install failed for {logicalName}", ex);
                    }
                }

                Logger.Info(Source, $"trigger install complete: {result}");
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "trigger install aborted", ex);
                result.Failed++;
            }

            return result;
        }

        /// <summary>
        /// Resolves the real table name from information_schema so we never guess
        /// at casing, and so a missing table is skipped rather than fatal.
        /// </summary>
        private static async Task<string?> ResolveTableNameAsync(
            DbConnection conn, string logicalName, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT TABLE_NAME FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@name)
LIMIT 1;";
            AddParam(cmd, "@name", logicalName);

            object? value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return value as string;
        }

        private static async Task<string?> ResolvePrimaryKeyAsync(
            DbConnection conn, string table, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @table
  AND CONSTRAINT_NAME = 'PRIMARY';";
            AddParam(cmd, "@table", table);

            var columns = new List<string>();

            using (DbDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false))
            {
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    columns.Add(reader.GetString(0));
            }

            // A composite key cannot be represented by the outbox's single row_pk.
            return columns.Count == 1 ? columns[0] : null;
        }

        private static async Task EnsureTriggerAsync(
            DbConnection conn, string table, string pk, string suffix,
            string dmlEvent, string rowAlias, string op,
            TriggerInstallResult result, CancellationToken ct)
        {
            string name = $"trg_{table}_{suffix}";
            string body = BuildBody(table, pk, rowAlias, op);

            string? existing = await GetTriggerBodyAsync(conn, name, ct).ConfigureAwait(false);

            if (existing != null)
            {
                if (existing.Contains(BodyVersion))
                {
                    result.AlreadyCurrent++;
                    return;
                }

                // An older generation of this trigger: replace it.
                await ExecuteAsync(conn, $"DROP TRIGGER IF EXISTS `{name}`;", ct).ConfigureAwait(false);
                await ExecuteAsync(conn, BuildCreate(name, table, dmlEvent, body), ct).ConfigureAwait(false);
                result.Replaced++;
                Logger.Info(Source, $"replaced outdated trigger {name}");
                return;
            }

            await ExecuteAsync(conn, BuildCreate(name, table, dmlEvent, body), ct).ConfigureAwait(false);
            result.Created++;
        }

        private static string BuildBody(string table, string pk, string rowAlias, string op) => $@"
BEGIN
  -- {BodyVersion}
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION BEGIN END;
  INSERT INTO sync_outbox (table_name, row_pk, op)
  VALUES ('{table}', {rowAlias}.`{pk}`, '{op}');
END";

        /// <summary>
        /// No DELIMITER handling is needed here. DELIMITER is a mysql command-line
        /// client construct; a driver sends CREATE TRIGGER as one statement and the
        /// server parses the BEGIN...END body itself.
        /// </summary>
        private static string BuildCreate(string name, string table, string dmlEvent, string body) =>
            $"CREATE TRIGGER `{name}` AFTER {dmlEvent} ON `{table}` FOR EACH ROW {body}";

        private static async Task<string?> GetTriggerBodyAsync(
            DbConnection conn, string name, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT ACTION_STATEMENT FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME = @name
LIMIT 1;";
            AddParam(cmd, "@name", name);

            object? value = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return value as string;
        }

        private static async Task ExecuteAsync(DbConnection conn, string sql, CancellationToken ct)
        {
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// Removes every trigger this installer owns. Used by the kill switch so a
        /// branch can be returned to exactly its previous behaviour.
        /// </summary>
        public static async Task<int> RemoveAllAsync(CancellationToken ct = default)
        {
            int removed = 0;

            try
            {
                using var db = new AppDbContext();
                DbConnection conn = db.Database.GetDbConnection();

                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync(ct).ConfigureAwait(false);

                foreach (string logicalName in SyncTables.Operational)
                {
                    string? table = await ResolveTableNameAsync(conn, logicalName, ct).ConfigureAwait(false);
                    if (table == null) continue;

                    foreach (string suffix in new[] { "ai", "au", "ad" })
                    {
                        await ExecuteAsync(conn, $"DROP TRIGGER IF EXISTS `trg_{table}_{suffix}`;", ct)
                            .ConfigureAwait(false);
                        removed++;
                    }
                }

                Logger.Info(Source, $"removed sync triggers ({removed} drop statements)");
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "could not remove sync triggers", ex);
            }

            return removed;
        }
    }
}
