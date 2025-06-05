using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightMaster.Models;

namespace WeightMaster.Utiles
{
    public static class Supporter
    {
        public static void handleEquationSt2(GreenLeafPostModel g, int bagWeight) 
        {
            //int watered = 0;
            //int matured = 0;
            //int spoiled = 0;
            //int rejected = 0;

            //int totalDeduction = 0;

            double TotalWeight = g.total_weight;

            double NomalLeafWeight = g.nomal_leaf_weight;
            double GoldLeafWeight = g.gold_leaf_weight;

            int FinalGoldLeafCount = g.final_gold_leaf_count;
            int FinalGreenLeafCount = g.final_green_leaf_count;

        }
    }
}
