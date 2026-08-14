using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    /// <summary>
    /// One pending change notification, appended by a database trigger.
    /// Deliberately minimal: the triggers that write this run inside the
    /// application's own write transaction, so anything expensive or lockable
    /// here would be felt at the scale.
    /// </summary>
    [Table("sync_outbox")]
    public class SyncOutboxEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("seq")]
        public long Seq { get; set; }

        [Column("table_name")]
        public string TableName { get; set; } = string.Empty;

        [Column("row_pk")]
        public long RowPk { get; set; }

        /// <summary>I = insert, U = update, D = delete.</summary>
        [Column("op")]
        public string Op { get; set; } = string.Empty;

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; }
    }
}
