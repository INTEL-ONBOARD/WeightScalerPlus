using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    public class RunLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool Status { get; set; }

        public DateTime Date { get; set; }

        public DateTime LastUpdated { get; set; }

        // Foreign key to TransactionLogBlockModel
        public int? TransactionId { get; set; }

        [ForeignKey("TransactionId")]
        public TransactionLogBlockModel? Transaction { get; set; }

        // Foreign key to FinalTransactionBlockModel
        public int? FinalTransactionId { get; set; }

        [ForeignKey("FinalTransactionId")]
        public FinalTransactionBlockModel? FinalTransaction { get; set; }
    }
}
