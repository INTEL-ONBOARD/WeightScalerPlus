using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WeightMaster.Models
{
    public class TransactionLogBlockModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? linename { get; set; }
        public string? transportagent { get; set; }
        public string? company { get; set; }
        public string? leaf_weight_officer { get; set; }
        public string? superviosr { get; set; } // Corrected based on your JSON

        public string? barcode_details { get; set; }
        public string? name_with_initials { get; set; }
        public string? phone_number { get; set; }
        public string? date { get; set; } // Assuming this should remain as string for format "YYYY-MM-DD"

        public int box_count { get; set; }
        public int bag_count { get; set; }
        public double real_value { get; set; }

        public int maximum_nomal_leaf_weight { get; set; }
        public int total_leaf_weight { get; set; }
        public int actual_nomal_leaf_weight { get; set; }
        public int total_gold_leaf_weight { get; set; }

        public int water { get; set; }
        public int morapuwata { get; set; }
        public int thambimata { get; set; }
        public int reject { get; set; }
        public int box_weight { get; set; }

        public int final_green_leaf_count { get; set; }
        public int final_gold_leaf_count { get; set; }
    }
}
