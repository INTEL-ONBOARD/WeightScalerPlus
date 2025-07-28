using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using WeightMaster.Models;

namespace WeightMaster.utils
{
    public static class BlockModelConverter
    {
        public static List<FinalTransactionReportBlockModel> CombineBlockModels(
            List<FinalTransactionBlockModel> lineReportData,
            List<TransactionLogBlockModel> boxReportData)
        {
            var convertedLineData = lineReportData.Select(item => new FinalTransactionReportBlockModel
            {
                Id = item.Id,
                linename = item.linename,
                transportagent = item.transportagent,
                company = item.company,
                leaf_weight_officer = item.leaf_weight_officer,
                superviosr = item.superviosr,
                barcode_details = item.barcode_details,
                name_with_initials = item.name_with_initials,
                phone_number = item.phone_number,
                date = item.date,
                bag_count = item.bag_count,
                real_value = item.real_value,
                maximum_nomal_leaf_weight = item.maximum_nomal_leaf_weight,
                total_leaf_weight = item.total_leaf_weight,
                actual_nomal_leaf_weight = item.actual_nomal_leaf_weight,
                total_gold_leaf_weight = item.total_gold_leaf_weight,
                water = item.water,
                morapuwata = item.morapuwata,
                thambimata = item.thambimata,
                reject = item.reject,
                bag_weight = item.bag_weight,
                final_green_leaf_count = item.final_green_leaf_count,
                final_gold_leaf_count = item.final_gold_leaf_count,
                // Box-specific properties not in lineReportData
                box_count = 0,
                box_weight = 0
            });

            var convertedBoxData = boxReportData.Select(item => new FinalTransactionReportBlockModel
            {
                Id = item.Id,
                linename = item.linename,
                transportagent = item.transportagent,
                company = item.company,
                leaf_weight_officer = item.leaf_weight_officer,
                superviosr = item.superviosr,
                barcode_details = item.barcode_details,
                name_with_initials = item.name_with_initials,
                phone_number = item.phone_number,
                date = item.date,
                box_count = item.box_count,
                bag_count = item.bag_count,
                real_value = (float)item.real_value, // Cast double to float
                maximum_nomal_leaf_weight = item.maximum_nomal_leaf_weight,
                total_leaf_weight = item.total_leaf_weight,
                actual_nomal_leaf_weight = item.actual_nomal_leaf_weight,
                total_gold_leaf_weight = item.total_gold_leaf_weight,
                water = item.water,
                morapuwata = item.morapuwata,
                thambimata = item.thambimata,
                reject = item.reject,
                box_weight = item.box_weight,
                final_green_leaf_count = item.final_green_leaf_count,
                final_gold_leaf_count = item.final_gold_leaf_count,
                // Bag_weight not available in boxReportData
                bag_weight = 0
            });

            return convertedLineData.Concat(convertedBoxData).ToList();
        }

        public static List<FinalTransactionBlockModel> ToFinalTransactionBlockModel(
            List<FinalTransactionBlockModel> lineReportData,
            List<TransactionLogBlockModel> boxReportData)
        {
            var convertedLineData = lineReportData.Select(item => new FinalTransactionBlockModel
            {
                Id = item.Id,
                linename = item.linename,
                transportagent = item.transportagent,
                company = item.company,
                leaf_weight_officer = item.leaf_weight_officer,
                superviosr = item.superviosr,
                barcode_details = item.barcode_details,
                name_with_initials = item.name_with_initials,
                phone_number = item.phone_number,
                date = item.date,

                bag_count = item.bag_count,
                real_value = item.real_value,
                maximum_nomal_leaf_weight = item.maximum_nomal_leaf_weight,
                total_leaf_weight = item.total_leaf_weight,
                actual_nomal_leaf_weight = item.actual_nomal_leaf_weight,
                total_gold_leaf_weight = item.total_gold_leaf_weight,

                water = item.water,
                morapuwata = item.morapuwata,
                thambimata = item.thambimata,
                reject = item.reject,
                bag_weight = item.bag_weight,

                final_green_leaf_count = item.final_green_leaf_count,
                final_gold_leaf_count = item.final_gold_leaf_count,
            });

            var convertedBoxData = boxReportData.Select(item => new FinalTransactionBlockModel
            {
                Id = item.Id,
                linename = item.linename,
                transportagent = item.transportagent,
                company = item.company,
                leaf_weight_officer = item.leaf_weight_officer,
                superviosr = item.superviosr,
                barcode_details = item.barcode_details,
                name_with_initials = item.name_with_initials,
                phone_number = item.phone_number,
                date = item.date,

                // Bag_weight not available in boxReportData
                bag_weight = 0,
                bag_count = item.bag_count,
                real_value = (float)item.real_value, // Cast double to float
                maximum_nomal_leaf_weight = item.maximum_nomal_leaf_weight,
                total_leaf_weight = item.total_leaf_weight,
                actual_nomal_leaf_weight = item.actual_nomal_leaf_weight,
                total_gold_leaf_weight = item.total_gold_leaf_weight,

                water = item.water,
                morapuwata = item.morapuwata,
                thambimata = item.thambimata,
                reject = item.reject,

                final_green_leaf_count = item.final_green_leaf_count,
                final_gold_leaf_count = item.final_gold_leaf_count,
            });

            return convertedLineData.Concat(convertedBoxData).ToList();
        }
    }
}
