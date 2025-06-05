using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WeightMaster.Models;

namespace WeightMaster.Utiles
{
    public static class Supporter
    {
        public static void handleEquationSt2(GreenLeafPostModel g, int acceptedSackWeight) 
        {


            double TotalWeight = g.total_weight; //const
            //double NomalLeafWeight = g.NomalLeafWeight; //const
            //double GoldLeafWeight = g.GoldLeafWeight; //const

            //int FinalGoldLeafCount = g.FinalGoldLeafCount;
            //int FinalGreenLeafCount = g.FinalGreenLeafCount;

            //this is the value that get returned
            GreenLeafPostModel newG = g;

            /*// Check if controls exist (avoids NullReferenceException during initialization)
            if (wateredTxt_st2 == null || rejectedTxt_st2 == null || maturedTxt_st2 == null || spoiledTxt_st2 == null || currentNormalLeafWeight_st2 == null || acceptedSackWeightTxt_st2 == null)
                return;
            // Parse values (handle empty/invalid input)
            if (!double.TryParse(wateredTxt_st2.Text, out double watered) || watered < 0)
                watered = 0;
            if (!double.TryParse(rejectedTxt_st2.Text, out double rejected) || rejected < 0)
                rejected = 0;
            if (!double.TryParse(maturedTxt_st2.Text, out double matured) || matured < 0)
                matured = 0;

            //MessageBox.Show("matured: "+matured);
            if (!double.TryParse(spoiledTxt_st2.Text, out double spoiled) || spoiled < 0)
                spoiled = 0;
            //if (!double.TryParse(nBoxesTxt_st2.Text, out double nBoxes) || spoiled < 0)
            //    nBoxes = 0;
            if (!double.TryParse(acceptedSackWeightTxt_st2.Text, out double acceptedSackWeight) || acceptedSackWeight < 0)
                acceptedSackWeight = 0;*/
            //double boxWeights = nBoxes * singleBoxWeight;
            // Calculate total
            double totalDeductions = g.wathurata + g.rejected + g.morapuwata + g.thambimata + acceptedSackWeight; //box weight doesn't sum up to the total decuctions the way i did calculation in st1 in both apis

            //choose deduction type between green leaves or golden leaves based on total
            //if normal weight doesn't exceeds total deduction(no need to update golden leaf weights)
            if (totalDeductions <= g.nomal_leaf_weight)
            {
                newG.final_green_leaf_count = (int)(g.nomal_leaf_weight - totalDeductions);
                newG.final_gold_leaf_count = (int)g.gold_leaf_weight;
                MessageBox.Show($"green:{newG.final_green_leaf_count} = normal:{g.nomal_leaf_weight} - deduc:{totalDeductions}");
                //update helper value
                //currentTotalDeduction_st2 = totalDeductions;
            }
            //if normal weight doesn't exceeds total deduction(now you need to update both golden leaf weights & normal leaf weights)
            else if (totalDeductions > g.nomal_leaf_weight)
            {
                newG.final_green_leaf_count = 0;
                newG.final_gold_leaf_count = (int)(g.gold_leaf_weight - (totalDeductions - g.nomal_leaf_weight));
                //update helper value
                //currentTotalDeduction_st2 = totalDeductions;
                
            }
            MessageBox.Show($"" +
                $"avail. green leaf:{newG.final_green_leaf_count} \n" +
                $"avail. gold leaf:{newG.final_gold_leaf_count} \n " +
                $"bag weight: {newG.bag_weight}" +
                $"");


        }
    }
}






/*public static void handleEquationSt2(GreenLeafPostModel g, int acceptedSackWeight)
{
    int watered = 0; //new vals
    int matured = 0; //new vals
    int spoiled = 0; //new vals
    int rejected = 0; //new vals
    double boxWeight = g.BoxWeight;
    //int totalDeduction = 0;

    double TotalWeight = g.TotalWeight; //const

    double NomalLeafWeight = g.NomalLeafWeight; //const
    double GoldLeafWeight = g.GoldLeafWeight; //const

    int FinalGoldLeafCount = g.FinalGoldLeafCount;
    int FinalGreenLeafCount = g.FinalGreenLeafCount;

    *//*// Check if controls exist (avoids NullReferenceException during initialization)
    if (wateredTxt_st2 == null || rejectedTxt_st2 == null || maturedTxt_st2 == null || spoiledTxt_st2 == null || currentNormalLeafWeight_st2 == null || acceptedSackWeightTxt_st2 == null)
        return;
    // Parse values (handle empty/invalid input)
    if (!double.TryParse(wateredTxt_st2.Text, out double watered) || watered < 0)
        watered = 0;
    if (!double.TryParse(rejectedTxt_st2.Text, out double rejected) || rejected < 0)
        rejected = 0;
    if (!double.TryParse(maturedTxt_st2.Text, out double matured) || matured < 0)
        matured = 0;

    //MessageBox.Show("matured: "+matured);
    if (!double.TryParse(spoiledTxt_st2.Text, out double spoiled) || spoiled < 0)
        spoiled = 0;
    //if (!double.TryParse(nBoxesTxt_st2.Text, out double nBoxes) || spoiled < 0)
    //    nBoxes = 0;
    if (!double.TryParse(acceptedSackWeightTxt_st2.Text, out double acceptedSackWeight) || acceptedSackWeight < 0)
        acceptedSackWeight = 0;*//*
    //double boxWeights = nBoxes * singleBoxWeight;
    // Calculate total
    double totalDeductions = watered + rejected + matured + spoiled + acceptedSackWeight + boxWeight;

    //choose deduction type between green leaves or golden leaves based on total
    //if normal weight doesn't exceeds total deduction(no need to update golden leaf weights)
    if (totalDeductions <= NomalLeafWeight)
    {
        normalLeafWeightTxt_st2.Text = (currentNormalLeafWeight_st2 - totalDeductions).ToString();
        goldenLeafWeightTxt_st2.Text = currentGoldenLeafWeight_st2.ToString();
        //update helper value
        currentTotalDeduction_st2 = totalDeductions;
        blueText2.Text = currentTotalDeduction_st2.ToString();
    }
    //if normal weight doesn't exceeds total deduction(now you need to update both golden leaf weights & normal leaf weights)
    else if (totalDeductions > NomalLeafWeight)
    {
        normalLeafWeightTxt_st2.Text = "0";
        goldenLeafWeightTxt_st2.Text = (currentGoldenLeafWeight_st2 - (totalDeductions - currentNormalLeafWeight_st2)).ToString();
        //update helper value
        currentTotalDeduction_st2 = totalDeductions;
        blueText2.Text = currentTotalDeduction_st2.ToString();
    }



}*/
