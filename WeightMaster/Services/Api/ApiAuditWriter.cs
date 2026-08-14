using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;

namespace WeightMaster.Services.Api
{
    internal enum ApiAuditKind { Start, Finish }

    internal sealed class ApiAuditMessage
    {
        public ApiAuditKind Kind;
        public Guid Correlation;

        // Start
        public DateTime OccurredAt;
        public string Method = string.Empty;
        public string Endpoint = string.Empty;
        public string? Operation;
        public string? SourceTable;
        public long? SourceRowPk;
        public string RequestJson = string.Empty;

        // Finish
        public string Status = string.Empty;
        public int? HttpStatus;
        public string? ResponseJson;
        public string? ErrorMessage;
        public int DurationMs;
    }

    /// <summary>
    /// Writes api_post_log rows on a background thread.
    ///
    /// The HTTP call never waits on this. Every path is non-blocking and every
    /// failure is swallowed: losing an audit row is acceptable, delaying or
    /// breaking an API call is not.
    /// </summary>
    public static class ApiAuditWriter
    {
        private const string Source = "ApiAudit";
        private const int QueueCapacity = 2000;

        private static Channel<ApiAuditMessage>? _channel;
        private static CancellationTokenSource? _cts;
        private static Task? _pump;

        /// <summary>Correlation id -> the seq the Start row was written as.</summary>
        private static readonly ConcurrentDictionary<Guid, long> _seqByCorrelation = new();

        private static long _dropped;

        public static bool IsRunning => _channel != null;

        /// <summary>Number of audit messages dropped because the queue was full.</summary>
        public static long Dropped => Interlocked.Read(ref _dropped);

        public static void Start()
        {
            if (_channel != null) return;

            // DropWrite: when the queue is full the newest message is discarded
            // rather than blocking the caller. The HTTP path must never wait.
            _channel = Channel.CreateBounded<ApiAuditMessage>(
                new BoundedChannelOptions(QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropWrite,
                    SingleReader = true
                });

            _cts = new CancellationTokenSource();
            _pump = Task.Run(() => PumpAsync(_channel.Reader, _cts.Token));

            Logger.Info(Source, "api audit writer started");
        }

        public static async Task StopAsync()
        {
            if (_channel == null) return;

            _channel.Writer.TryComplete();

            try
            {
                if (_pump != null)
                    await _pump.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Warn(Source, $"writer did not stop cleanly: {ex.Message}");
            }
            finally
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
                _channel = null;
                _pump = null;
                _seqByCorrelation.Clear();
            }
        }

        internal static void Enqueue(ApiAuditMessage message)
        {
            Channel<ApiAuditMessage>? channel = _channel;
            if (channel == null) return;

            if (!channel.Writer.TryWrite(message))
                Interlocked.Increment(ref _dropped);
        }

        private static async Task PumpAsync(ChannelReader<ApiAuditMessage> reader, CancellationToken ct)
        {
            try
            {
                await foreach (ApiAuditMessage msg in reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        if (msg.Kind == ApiAuditKind.Start) await WriteStartAsync(msg, ct).ConfigureAwait(false);
                        else                                await WriteFinishAsync(msg, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // One bad row must not stop the pump.
                        Logger.Error(Source, "could not write audit row", ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "audit writer stopped unexpectedly", ex);
            }
        }

        private static async Task WriteStartAsync(ApiAuditMessage msg, CancellationToken ct)
        {
            using var db = new AppDbContext();

            var row = new ApiPostLogEntry
            {
                OccurredAt  = msg.OccurredAt,
                Method      = msg.Method,
                Endpoint    = Truncate(msg.Endpoint, 255),
                Operation   = msg.Operation,
                SourceTable = msg.SourceTable,
                SourceRowPk = msg.SourceRowPk,
                RequestJson = msg.RequestJson,
                Status      = ApiPostLogEntry.StatusPending
            };

            db.ApiPostLog.Add(row);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            _seqByCorrelation[msg.Correlation] = row.Seq;
        }

        private static async Task WriteFinishAsync(ApiAuditMessage msg, CancellationToken ct)
        {
            // Messages are processed in order on a single reader, so the Start row
            // has always been written by the time its Finish arrives.
            if (!_seqByCorrelation.TryRemove(msg.Correlation, out long seq))
                return;

            using var db = new AppDbContext();

            ApiPostLogEntry? row = await db.ApiPostLog
                .FirstOrDefaultAsync(r => r.Seq == seq, ct)
                .ConfigureAwait(false);

            if (row == null) return;

            row.Status       = msg.Status;
            row.HttpStatus   = msg.HttpStatus;
            row.ResponseJson = msg.ResponseJson;
            row.ErrorMessage = Truncate(msg.ErrorMessage, 4000);
            row.DurationMs   = msg.DurationMs;

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static string? Truncate(string? value, int max)
        {
            if (value == null) return null;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
