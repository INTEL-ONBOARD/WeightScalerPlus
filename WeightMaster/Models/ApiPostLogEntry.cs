using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    /// <summary>
    /// A permanent record of one outbound API call and its outcome.
    ///
    /// Before this existed the payload went only to Debug.WriteLine, which the
    /// compiler strips from Release builds -- so production kept no record of
    /// what was actually sent to teacoop.lk.
    /// </summary>
    [Table("api_post_log")]
    public class ApiPostLogEntry
    {
        public const string StatusPending      = "pending";
        public const string StatusOk           = "ok";
        public const string StatusHttpError    = "http_error";
        public const string StatusNetworkError = "network_error";

        /// <summary>Response bodies are truncated to this many characters.</summary>
        public const int ResponseLimit = 64 * 1024;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("seq")]
        public long Seq { get; set; }

        [Column("occurred_at")]
        public DateTime OccurredAt { get; set; }

        [Column("method")]
        public string Method { get; set; } = string.Empty;

        [Column("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>Logical operation, e.g. "greenleaf". Inferred from the URL when not supplied.</summary>
        [Column("operation")]
        public string? Operation { get; set; }

        /// <summary>Local table this call was about, e.g. "greenleafposts".</summary>
        [Column("source_table")]
        public string? SourceTable { get; set; }

        /// <summary>Primary key of the row this call was about.</summary>
        [Column("source_row_pk")]
        public long? SourceRowPk { get; set; }

        /// <summary>The exact bytes sent on the wire, after every transformation.</summary>
        [Column("request_json")]
        public string RequestJson { get; set; } = string.Empty;

        /// <summary>
        /// A row left at <see cref="StatusPending"/> is a call that never came
        /// back -- direct evidence of a hang.
        /// </summary>
        [Column("status")]
        public string Status { get; set; } = StatusPending;

        [Column("http_status")]
        public int? HttpStatus { get; set; }

        [Column("response_json")]
        public string? ResponseJson { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("duration_ms")]
        public int? DurationMs { get; set; }

        [Column("attempt")]
        public int Attempt { get; set; } = 1;
    }
}
