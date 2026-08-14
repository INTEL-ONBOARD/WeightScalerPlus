using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    /// <summary>
    /// Per-table bookkeeping for the sync worker: how far the watermark has
    /// advanced, whether the one-time backfill finished, and (for the master
    /// tables) the last content hash we pushed.
    /// </summary>
    [Table("sync_state")]
    public class SyncStateEntry
    {
        [Key]
        [Column("table_name")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>Highest primary key confirmed present in the cloud.</summary>
        [Column("watermark_id")]
        public long WatermarkId { get; set; }

        [Column("backfill_done")]
        public bool BackfillDone { get; set; }

        /// <summary>Resume point for the one-time history upload.</summary>
        [Column("backfill_pos")]
        public long BackfillPos { get; set; }

        [Column("last_sweep_at")]
        public DateTime? LastSweepAt { get; set; }

        /// <summary>
        /// Whole-table hash, used only by the master tables, which are wiped and
        /// reinserted wholesale and so cannot be tracked row by row.
        /// </summary>
        [Column("content_hash")]
        public string? ContentHash { get; set; }
    }
}
