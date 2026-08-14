using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WeightMaster.Models;

namespace WeightMaster.Services.Api
{
    /// <summary>
    /// Records every request made through this HttpClient into api_post_log.
    ///
    /// Three rules, in priority order over usefulness:
    ///   1. Never throw. Any capture failure is swallowed; the call proceeds
    ///      exactly as it would without this handler.
    ///   2. Never wait on the database. Rows are handed to a background writer.
    ///   3. Never store credentials. Headers are not recorded at all, and bodies
    ///      of authentication calls are replaced with a placeholder.
    /// </summary>
    public sealed class ApiAuditHandler : DelegatingHandler
    {
        private const string Source = "ApiAudit";

        /// <summary>Guards the database against a pathological payload.</summary>
        private const int RequestLimit = 256 * 1024;

        public ApiAuditHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Disabled, or not started: behave as if this handler did not exist.
            if (!ApiAuditWriter.IsRunning)
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            Guid correlation = Guid.NewGuid();
            bool recorded = false;
            var clock = Stopwatch.StartNew();

            try
            {
                recorded = await TryRecordStartAsync(request, correlation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "could not record request start", ex);
            }

            HttpResponseMessage response;

            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (recorded) TryRecordOutcome(correlation, ApiPostLogEntry.StatusNetworkError,
                                               null, null, ex.Message, clock);
                throw;   // caller behaviour is unchanged
            }

            try
            {
                if (recorded) await TryRecordResponseAsync(correlation, response, clock).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(Source, "could not record response", ex);
            }

            return response;
        }

        private static async Task<bool> TryRecordStartAsync(HttpRequestMessage request, Guid correlation)
        {
            string url = request.RequestUri?.ToString() ?? string.Empty;

            // Precedence: an explicit per-request tag, then the ambient scope,
            // then a guess from the URL. Untagged call sites still get useful rows.
            ApiAuditScope? scope = ApiAuditScope.Current;

            string operation =
                request.Options.TryGetValue(ApiAuditContext.OperationKey, out string? op) ? op
                : scope?.Operation
                ?? ApiAuditContext.InferOperation(url);

            string? sourceTable =
                request.Options.TryGetValue(ApiAuditContext.SourceTableKey, out string? st) ? st
                : scope?.SourceTable;

            long? sourceRowPk =
                request.Options.TryGetValue(ApiAuditContext.SourceRowPkKey, out long pk) ? pk
                : scope?.SourceRowPk;

            string body = await ReadRequestBodyAsync(request, operation).ConfigureAwait(false);

            ApiAuditWriter.Enqueue(new ApiAuditMessage
            {
                Kind        = ApiAuditKind.Start,
                Correlation = correlation,
                OccurredAt  = DateTime.Now,
                Method      = request.Method.Method,
                Endpoint    = url,
                Operation   = operation,
                SourceTable = sourceTable,
                SourceRowPk = sourceRowPk,
                RequestJson = body
            });

            return true;
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequestMessage request, string operation)
        {
            if (request.Content == null) return string.Empty;

            // Credentials must never reach the log, local or cloud.
            if (operation == "login")
                return "<redacted: authentication request>";

            try
            {
                string body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

                return body.Length <= RequestLimit
                    ? body
                    : body.Substring(0, RequestLimit) + "…<truncated>";
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static async Task TryRecordResponseAsync(
            Guid correlation, HttpResponseMessage response, Stopwatch clock)
        {
            int code = (int)response.StatusCode;
            string status = response.IsSuccessStatusCode
                ? ApiPostLogEntry.StatusOk
                : ApiPostLogEntry.StatusHttpError;

            string? body = await ReadResponseBodyAsync(response).ConfigureAwait(false);

            TryRecordOutcome(correlation, status, code, body,
                             response.IsSuccessStatusCode ? null : response.ReasonPhrase, clock);
        }

        /// <summary>
        /// Reads the response body only when its length is known and small enough
        /// to buffer safely.
        ///
        /// Buffering a response of unknown or excessive length risks consuming a
        /// stream the caller still needs. A missing response body in the log is a
        /// small loss; a broken deserialisation in the app is not.
        /// </summary>
        private static async Task<string?> ReadResponseBodyAsync(HttpResponseMessage response)
        {
            if (response.Content == null) return null;

            long? length = response.Content.Headers.ContentLength;

            if (length == null || length > ApiPostLogEntry.ResponseLimit)
                return length == null ? null : "<not captured: response too large>";

            try
            {
                await response.Content.LoadIntoBufferAsync(ApiPostLogEntry.ResponseLimit)
                              .ConfigureAwait(false);

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private static void TryRecordOutcome(
            Guid correlation, string status, int? httpStatus,
            string? responseBody, string? error, Stopwatch clock)
        {
            clock.Stop();

            ApiAuditWriter.Enqueue(new ApiAuditMessage
            {
                Kind         = ApiAuditKind.Finish,
                Correlation  = correlation,
                Status       = status,
                HttpStatus   = httpStatus,
                ResponseJson = responseBody,
                ErrorMessage = error,
                DurationMs   = (int)clock.ElapsedMilliseconds
            });
        }
    }
}
