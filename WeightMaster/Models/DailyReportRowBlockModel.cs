using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightMaster.Models
{
    public class DailyReportRowBlockModel
    {
        public int Id { get; set; }
        public string? linename { get; set; }
        //public string? date { get; set; }


        public int bag_count { get; set; } = 0;
        public int maximum_nomal_leaf_weight { get; set; } = 0;
        public int total_leaf_weight { get; set; } = 0;
        public int actual_nomal_leaf_weight { get; set; } = 0;
        public int total_gold_leaf_weight { get; set; } = 0;
        public int water { get; set; } = 0;
        public int morapuwata { get; set; } = 0;
        public int thambimata { get; set; } = 0;
        public int reject { get; set; } = 0;
        public int bag_weight { get; set; } = 0;
        public int final_green_leaf_count { get; set; } = 0;
        public int final_gold_leaf_count { get; set; } = 0;
        public float real_value { get; set; } = 0;
    }
}
