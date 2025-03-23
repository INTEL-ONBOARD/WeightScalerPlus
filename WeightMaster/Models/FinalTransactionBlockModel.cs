using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class FinalTransactionBlockModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? linename { get; set; }
        public string? transportagent { get; set; }
        public string? company { get; set; }
        public string? leaf_weight_officer { get; set; }
        public string? superviosr { get; set; }
        public string? barcode_details { get; set; }
        public string? name_with_initials { get; set; }
        public string? phone_number { get; set; }
        public string? date { get; set; }
        public int bag_count { get; set; }
        public int maximum_nomal_leaf_weight { get; set; }
        public int total_leaf_weight { get; set; }
        public int actual_nomal_leaf_weight { get; set; }
        public int total_gold_leaf_weight { get; set; }
        public int water { get; set; }
        public int morapuwata { get; set; }
        public int thambimata { get; set; }
        public int reject { get; set; }
        public int bag_weight { get; set; }
        public int final_green_leaf_count { get; set; }
        public int final_gold_leaf_count { get; set; }
        public float real_value { get; set; }
    }
}
