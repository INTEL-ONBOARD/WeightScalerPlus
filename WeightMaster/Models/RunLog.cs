using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public TransactionLogBlockModel? Transaction { get; set; }
        public FinalTransactionBlockModel? FinalTransaction { get; set; }

    } 
}