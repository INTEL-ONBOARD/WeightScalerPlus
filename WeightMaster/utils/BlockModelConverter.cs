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


        public static GreenLeafPostModel? Sum(List<GreenLeafPostModel> items)
        {
            if (items == null || items.Count == 0)
                return null;

            var first = items[0];

            var result = new GreenLeafPostModel
            {
                // copy metadata from first item
                id = first.id,
                leaf_handover_date = first.leaf_handover_date,
                factory = first.factory,
                transportlinename = first.transportlinename,
                transportagent = first.transportagent,
                leaf_weight_officer = first.leaf_weight_officer,
                supervisor = first.supervisor,
                membernumber = first.membernumber,
                premembernumber = first.premembernumber,
                created_user = first.created_user,
                updated_user = first.updated_user,

                // summed numeric fields
                bag_count = items.Sum(x => x.bag_count),
                box_count = items.Sum(x => x.box_count),
                real_weight = items.Sum(x => x.real_weight),
                total_weight = items.Sum(x => x.total_weight),
                nomal_leaf_weight = items.Sum(x => x.nomal_leaf_weight),
                gold_leaf_weight = items.Sum(x => x.gold_leaf_weight),
                wathurata = items.Sum(x => x.wathurata),
                morapuwata = items.Sum(x => x.morapuwata),
                thambimata = items.Sum(x => x.thambimata),
                rejected = items.Sum(x => x.rejected),
                bag_weight = items.Sum(x => x.bag_weight),
                box_weight = items.Sum(x => x.box_weight),
                final_green_leaf_count = items.Sum(x => x.final_green_leaf_count),
                final_gold_leaf_count = items.Sum(x => x.final_gold_leaf_count)
            };

            return result;
        }

        public static List<TransactionLogBlockModel> filterTransactionDataByLine( List<TransactionLogBlockModel> list, string lineName)
        {
            // Defensive checks
            if (list == null || list.Count == 0)
                return new List<TransactionLogBlockModel>();

            if (string.IsNullOrWhiteSpace(lineName))
                return new List<TransactionLogBlockModel>(); // if you prefer to return the original list instead, change this to `return new List<TransactionLogBlockModel>(list);`

            string target = lineName.Trim();

            var filteredList = list
                .Where(item => !string.IsNullOrWhiteSpace(item?.linename)
                               && string.Equals(item.linename.Trim(), target, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return filteredList;
        }

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

                bag_count = item.bag_count,
                real_value = (float)item.real_value,  // Cast double to float
                maximum_nomal_leaf_weight = item.maximum_nomal_leaf_weight,
                total_leaf_weight = item.total_leaf_weight,
                actual_nomal_leaf_weight = item.actual_nomal_leaf_weight,
                total_gold_leaf_weight = item.total_gold_leaf_weight,

                water = item.water,
                morapuwata = item.morapuwata,
                thambimata = item.thambimata,
                reject = item.reject,
                bag_weight = 0,  // Not available in TransactionLogBlockModel
                final_green_leaf_count = item.final_green_leaf_count,
                final_gold_leaf_count = item.final_gold_leaf_count,
            });

            return convertedLineData.Concat(convertedBoxData).ToList();
        }
    }
}
