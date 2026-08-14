using System;
using System.Threading;

namespace WeightMaster.Services.Api
{
    /// <summary>
    /// Ambient description of what the current API call is about, so the audit
    /// row can point back at the database row that caused it.
    ///
    /// This exists instead of extra parameters on ApiClient.PostAsync because
    /// that class already carries two overloads distinguished only by optional
    /// arguments; adding another optional parameter risks making existing call
    /// sites ambiguous. An AsyncLocal flows through the await chain into the
    /// handler without changing a single signature.
    ///
    ///     using (ApiAuditScope.For("greenleaf", "greenleafposts", post.id))
    ///     {
    ///         await client.PostAsync&lt;object&gt;(url, payload);
    ///     }
    /// </summary>
    public sealed class ApiAuditScope : IDisposable
    {
        private static readonly AsyncLocal<ApiAuditScope?> _current = new();

        public string Operation { get; }
        public string? SourceTable { get; }
        public long? SourceRowPk { get; }

        private readonly ApiAuditScope? _previous;
        private bool _disposed;

        private ApiAuditScope(string operation, string? sourceTable, long? sourceRowPk)
        {
            Operation   = operation;
            SourceTable = sourceTable;
            SourceRowPk = sourceRowPk;

            _previous = _current.Value;
            _current.Value = this;
        }

        internal static ApiAuditScope? Current => _current.Value;

        public static ApiAuditScope For(string operation, string? sourceTable = null, long? sourceRowPk = null)
            => new ApiAuditScope(operation, sourceTable, sourceRowPk);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = _previous;
        }
    }
}
