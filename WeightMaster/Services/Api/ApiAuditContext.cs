using System.Net.Http;

namespace WeightMaster.Services.Api
{
    /// <summary>
    /// Lets a call site tell the audit handler what a request is about, so the
    /// logged row can point back at the database row it came from.
    ///
    /// Entirely optional. When a call site says nothing, the handler falls back
    /// to inferring the operation from the URL.
    /// </summary>
    public static class ApiAuditContext
    {
        public static readonly HttpRequestOptionsKey<string> OperationKey =
            new HttpRequestOptionsKey<string>("wm.audit.operation");

        public static readonly HttpRequestOptionsKey<string> SourceTableKey =
            new HttpRequestOptionsKey<string>("wm.audit.sourceTable");

        public static readonly HttpRequestOptionsKey<long> SourceRowPkKey =
            new HttpRequestOptionsKey<long>("wm.audit.sourceRowPk");

        /// <summary>Tag a request with the row it concerns. Never throws.</summary>
        public static HttpRequestMessage Describe(
            this HttpRequestMessage request,
            string operation,
            string? sourceTable = null,
            long? sourceRowPk = null)
        {
            request.Options.Set(OperationKey, operation);

            if (sourceTable != null)
                request.Options.Set(SourceTableKey, sourceTable);

            if (sourceRowPk.HasValue)
                request.Options.Set(SourceRowPkKey, sourceRowPk.Value);

            return request;
        }

        /// <summary>
        /// Best-effort operation name from the URL, used when a call site did not
        /// describe itself. Keeps older call sites useful without touching them.
        /// </summary>
        public static string InferOperation(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "unknown";

            string u = url.ToLowerInvariant();

            if (u.Contains("/greenleaf"))        return "greenleaf";
            if (u.Contains("/thirdpartylogin"))  return "login";
            if (u.Contains("/linemaster"))       return "linemaster";
            if (u.Contains("/members"))          return "members";
            if (u.Contains("/transaction"))      return "transaction";
            if (u.Contains("/user"))             return "users";

            return "other";
        }
    }
}
