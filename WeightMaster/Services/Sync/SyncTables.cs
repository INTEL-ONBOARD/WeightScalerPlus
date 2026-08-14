using System.Collections.Generic;

namespace WeightMaster.Services.Sync
{
    /// <summary>
    /// Which tables the mirror covers, and how each is handled.
    ///
    /// The names here are the EF DbSet property names, which is what EF Core maps
    /// tables to by convention in this project. Actual casing is resolved against
    /// information_schema at runtime rather than assumed.
    /// </summary>
    public static class SyncTables
    {
        /// <summary>
        /// Row-level mirrored. Stable identity keys, so triggers and per-row
        /// upserts are meaningful.
        /// </summary>
        public static readonly IReadOnlyList<string> Operational = new[]
        {
            "transactionData",
            "FinaltransactionData",
            "GreenLeafPosts",
            "PostStatus",
            "TransportBill",
            "RunLog",
            "UserLoginsLog",
            "memDbLog",
            "lineDbLog",
            "api_post_log"
        };

        /// <summary>
        /// Snapshot-mirrored, never triggered.
        ///
        /// ReplaceUsersAsync and ReplaceMembersAsync call RemoveRange over the
        /// whole table and reinsert it, so these identity keys change on every
        /// refresh and carry no meaning. Triggers here would fire tens of
        /// thousands of times per refresh for keys about to be discarded.
        /// </summary>
        public static readonly IReadOnlyList<string> Master = new[]
        {
            "UsersData",
            "MembersData",
            "lineMasterData"
        };

        /// <summary>Owned by the sync itself. Never mirrored, never triggered.</summary>
        public static readonly IReadOnlyList<string> Internal = new[]
        {
            "sync_outbox",
            "sync_state"
        };
    }
}
