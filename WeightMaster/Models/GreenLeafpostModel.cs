using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightMaster.Models
{
    public class GreenLeafPostModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        public string leaf_handover_date { get; set; } = string.Empty;

        public string factory { get; set; } = string.Empty;

        public string transportlinename { get; set; } = string.Empty;

        public string transportagent { get; set; } = string.Empty;

        public string leaf_weight_officer { get; set; } = string.Empty;

        public string supervisor { get; set; } = string.Empty;

        public string membernumber { get; set; } = string.Empty;

        public string? premembernumber { get; set; }

        public int bag_count { get; set; }

        public int box_count { get; set; }

        public double real_weight { get; set; }

        public double total_weight { get; set; }

        public double nomal_leaf_weight { get; set; }

        public double gold_leaf_weight { get; set; }

        public double wathurata { get; set; }

        public double morapuwata { get; set; }

        public double thambimata { get; set; }

        public double rejected { get; set; }

        public double bag_weight { get; set; }

        public double box_weight { get; set; }

        public int final_green_leaf_count { get; set; }

        public int final_gold_leaf_count { get; set; }

        public string created_user { get; set; } = string.Empty;

        public string updated_user { get; set; } = string.Empty;
    }
}
