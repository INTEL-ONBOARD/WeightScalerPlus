using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeightMaster.Services
{
    public class FinalTransactionService
    {
        private readonly AppDbContext _context;

        public FinalTransactionService(AppDbContext context)
        {
            _context = context;
        }

        // Map FinalTransactionBlockModel to a view model or other relevant model
        private FinalTransactionBlockModel MapTransactionToModel(FinalTransactionBlockModel transaction)
        {
            return new FinalTransactionBlockModel
            {
                Id = transaction.Id,
                linename = transaction.linename,
                transportagent = transaction.transportagent,
                company = transaction.company,
                leaf_weight_officer = transaction.leaf_weight_officer,
                superviosr = transaction.superviosr,
                barcode_details = transaction.barcode_details,
                name_with_initials = transaction.name_with_initials,
                phone_number = transaction.phone_number,
                date = transaction.date,
                bag_count = transaction.bag_count,
                maximum_nomal_leaf_weight = transaction.maximum_nomal_leaf_weight,
                total_leaf_weight = transaction.total_leaf_weight,
                actual_nomal_leaf_weight = transaction.actual_nomal_leaf_weight,
                total_gold_leaf_weight = transaction.total_gold_leaf_weight,
                water = transaction.water,
                morapuwata = transaction.morapuwata,
                thambimata = transaction.thambimata,
                reject = transaction.reject,
                bag_weight = transaction.bag_weight,
                final_green_leaf_count = transaction.final_green_leaf_count,
                final_gold_leaf_count = transaction.final_gold_leaf_count,
                real_value = transaction.real_value
            };
        }

        // Save transaction logs to the database
        public async Task SaveTransactionsAsync(List<FinalTransactionBlockModel> transactions)
        {
            var transactionLogModels = transactions.Select(t => MapTransactionToModel(t)).ToList();
            await _context.FinaltransactionData.AddRangeAsync(transactionLogModels);
            await _context.SaveChangesAsync();
        }

        // Replace all transactions in the database
        public async Task ReplaceTransactionsAsync(List<FinalTransactionBlockModel> transactions)
        {
            // Step 1: Clean the table (delete all rows)
            _context.FinaltransactionData.RemoveRange(_context.FinaltransactionData);

            // Step 2: Map and save new transactions
            var transactionLogModels = transactions.Select(t => MapTransactionToModel(t)).ToList();
            await _context.FinaltransactionData.AddRangeAsync(transactionLogModels);
            await _context.SaveChangesAsync();
        }

        // Get the total count of transactions
        public async Task<int> GetTransactionCountAsync()
        {
            return await _context.FinaltransactionData.CountAsync();
        }

        // Get all transactions by a specific transport agent
        public async Task<List<FinalTransactionBlockModel>> GetTransactionsByTransportAgentAsync(string transportagent)
        {
            return await _context.FinaltransactionData
                .Where(t => t.transportagent == transportagent)
                .ToListAsync();
        }

        // Get a specific transaction by ID
        public async Task<FinalTransactionBlockModel> GetTransactionByIdAsync(int id)
        {
            return await _context.FinaltransactionData.FirstOrDefaultAsync(t => t.Id == id);
        }

        // Delete a transaction by ID
        public async Task DeleteTransactionByIdAsync(int id)
        {
            var transaction = await GetTransactionByIdAsync(id);
            if (transaction != null)
            {
                _context.FinaltransactionData.Remove(transaction);
                await _context.SaveChangesAsync();
            }
        }

        // Update a specific transaction's details
        public async Task SetTransactionAsync(int id, FinalTransactionBlockModel updatedTransaction)
        {
            var existingTransaction = await GetTransactionByIdAsync(id);

            if (existingTransaction != null)
            {
                // Update the transaction fields with the provided data
                existingTransaction.Id = updatedTransaction.Id;
                existingTransaction.linename = updatedTransaction.linename;
                existingTransaction.transportagent = updatedTransaction.transportagent;
                existingTransaction.company = updatedTransaction.company;
                existingTransaction.leaf_weight_officer = updatedTransaction.leaf_weight_officer;
                existingTransaction.superviosr = updatedTransaction.superviosr;
                existingTransaction.barcode_details = updatedTransaction.barcode_details;
                existingTransaction.name_with_initials = updatedTransaction.name_with_initials;
                existingTransaction.phone_number = updatedTransaction.phone_number;
                existingTransaction.date = updatedTransaction.date;
                existingTransaction.bag_count = updatedTransaction.bag_count;
                existingTransaction.maximum_nomal_leaf_weight = updatedTransaction.maximum_nomal_leaf_weight;
                existingTransaction.total_leaf_weight = updatedTransaction.total_leaf_weight;
                existingTransaction.actual_nomal_leaf_weight = updatedTransaction.actual_nomal_leaf_weight;
                existingTransaction.total_gold_leaf_weight = updatedTransaction.total_gold_leaf_weight;
                existingTransaction.water = updatedTransaction.water;
                existingTransaction.morapuwata = updatedTransaction.morapuwata;
                existingTransaction.thambimata = updatedTransaction.thambimata;
                existingTransaction.reject = updatedTransaction.reject;
                existingTransaction.bag_weight = updatedTransaction.bag_weight;
                existingTransaction.final_green_leaf_count = updatedTransaction.final_green_leaf_count;
                existingTransaction.final_gold_leaf_count = updatedTransaction.final_gold_leaf_count;
                existingTransaction.real_value = updatedTransaction.real_value;

                // Save the updated transaction to the database
                await _context.SaveChangesAsync();
            }
        }

        // Add a new transaction
        public async Task<bool> AddTransactionAsync(FinalTransactionBlockModel newTransaction)
        {
            try
            {
                var transactionLogModel = MapTransactionToModel(newTransaction);

                // Add the transaction to the database
                await _context.FinaltransactionData.AddAsync(transactionLogModel);

                // Save changes to the database
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Get all transactions where bag_count is greater than 0
        public async Task<List<FinalTransactionBlockModel>> GetTransactionsWithBagCountGreaterThanZeroAsync()
        {
            return await _context.FinaltransactionData
                .Where(t => t.bag_count > 0)
                .ToListAsync();
        }

        public async Task<List<FinalTransactionBlockModel>> getDataForPrint(string linename_)
        {
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

            return await _context.FinaltransactionData
                .Where(t => t.bag_count > 0 && t.date == todayDate && t.linename == linename_)
                .ToListAsync();

        }
        public async Task<List<FinalTransactionBlockModel>> getDataForPrintOnCustomDate(string linename_,string date_)
        {
            return await _context.FinaltransactionData
                .Where(t => t.bag_count > 0 && t.date == date_ && t.linename == linename_)
                .ToListAsync();
        }

        // Get all transactions
        public async Task<List<FinalTransactionBlockModel>> GetAllTransactionsAsync()
        {
            return await _context.FinaltransactionData.ToListAsync();
        }

        // Get transactions filtered by linename
        public async Task<List<FinalTransactionBlockModel>> GetTransactionsByLineNameAsync(string linename)
        {
            return await _context.FinaltransactionData
                .Where(t => t.linename == linename)
                .ToListAsync();
        }

        // Get transactions filtered by barcode_details (member_id)
        public async Task<List<FinalTransactionBlockModel>> GetTransactionsByMemberIdAsync(string memberId)
        {
            return await _context.FinaltransactionData
                .Where(t => t.barcode_details == memberId)
                .ToListAsync();
        }

        // Get transactions filtered by both linename and barcode_details
        public async Task<List<FinalTransactionBlockModel>> GetTransactionsByLineAndMemberAsync(string linename, string memberId)
        {
            return await _context.FinaltransactionData
                .Where(t => t.linename == linename && t.barcode_details == memberId)
                .ToListAsync();
        }

        // Get distinct line names for dropdown
        public async Task<List<string>> GetDistinctLineNamesAsync()
        {
            return await _context.FinaltransactionData
                .Where(t => t.linename != null)
                .Select(t => t.linename!)
                .Distinct()
                .ToListAsync();
        }

        // Get line summary (aggregated data by line)
        public async Task<List<LineSummaryModel>> GetLineSummaryAsync()
        {
            return await _context.FinaltransactionData
                .Where(t => t.linename != null)
                .GroupBy(t => t.linename)
                .Select(g => new LineSummaryModel
                {
                    LineName = g.Key!,
                    TotalMembers = g.Select(t => t.barcode_details).Distinct().Count(),
                    TotalBags = g.Sum(t => t.bag_count),
                    TotalGoldLeafWeight = g.Sum(t => t.total_gold_leaf_weight),
                    TotalNormalLeafWeight = g.Sum(t => t.actual_nomal_leaf_weight),
                    TotalWeight = g.Sum(t => t.total_leaf_weight)
                })
                .ToListAsync();
        }

        // Get line summary filtered by linename
        public async Task<List<LineSummaryModel>> GetLineSummaryByLineNameAsync(string linename)
        {
            return await _context.FinaltransactionData
                .Where(t => t.linename == linename)
                .GroupBy(t => t.linename)
                .Select(g => new LineSummaryModel
                {
                    LineName = g.Key!,
                    TotalMembers = g.Select(t => t.barcode_details).Distinct().Count(),
                    TotalBags = g.Sum(t => t.bag_count),
                    TotalGoldLeafWeight = g.Sum(t => t.total_gold_leaf_weight),
                    TotalNormalLeafWeight = g.Sum(t => t.actual_nomal_leaf_weight),
                    TotalWeight = g.Sum(t => t.total_leaf_weight)
                })
                .ToListAsync();
        }

        // Get combined transactions (Completed from FinalTransaction + Queue from Transaction)
        public async Task<List<TransactionViewModel>> GetCombinedTransactionsAsync()
        {
            var result = new List<TransactionViewModel>();

            // Get all completed transactions from FinalTransactionData (with member_id)
            var completedTransactions = await _context.FinaltransactionData
                .Where(t => !string.IsNullOrEmpty(t.barcode_details))
                .Select(t => new TransactionViewModel
                {
                    Id = t.Id,
                    MemberId = t.barcode_details,
                    Name = t.name_with_initials,
                    LineName = t.linename,
                    BoxCount = 0, // FinalTransaction doesn't have box_count
                    BagCount = t.bag_count,
                    GoldLeafWeight = t.total_gold_leaf_weight,
                    NormalLeafWeight = t.actual_nomal_leaf_weight,
                    TotalWeight = t.total_leaf_weight,
                    Date = t.date,
                    Status = "Completed"
                })
                .ToListAsync();
            result.AddRange(completedTransactions);

            // Get transaction IDs that are already in FinalTransaction (via RunLog)
            var completedTransactionIds = await _context.RunLog
                .Where(r => r.FinalTransactionId != null && r.TransactionId != null)
                .Select(r => r.TransactionId)
                .ToListAsync();

            // Get queue transactions from TransactionData (not in FinalTransaction, with member_id)
            var queueTransactions = await _context.transactionData
                .Where(t => !string.IsNullOrEmpty(t.barcode_details) && !completedTransactionIds.Contains(t.Id))
                .Select(t => new TransactionViewModel
                {
                    Id = t.Id,
                    MemberId = t.barcode_details,
                    Name = t.name_with_initials,
                    LineName = t.linename,
                    BoxCount = t.box_count,
                    BagCount = t.bag_count,
                    GoldLeafWeight = t.total_gold_leaf_weight,
                    NormalLeafWeight = t.actual_nomal_leaf_weight,
                    TotalWeight = t.total_leaf_weight,
                    Date = t.date,
                    Status = "Queue"
                })
                .ToListAsync();
            result.AddRange(queueTransactions);

            return result;
        }

        // Get combined transactions filtered by linename
        public async Task<List<TransactionViewModel>> GetCombinedTransactionsByLineNameAsync(string linename)
        {
            var all = await GetCombinedTransactionsAsync();
            return all.Where(t => t.LineName == linename).ToList();
        }

        // Get combined transactions filtered by member_id
        public async Task<List<TransactionViewModel>> GetCombinedTransactionsByMemberIdAsync(string memberId)
        {
            var all = await GetCombinedTransactionsAsync();
            return all.Where(t => t.MemberId == memberId).ToList();
        }

        // Get combined transactions filtered by both linename and member_id
        public async Task<List<TransactionViewModel>> GetCombinedTransactionsByLineAndMemberAsync(string linename, string memberId)
        {
            var all = await GetCombinedTransactionsAsync();
            return all.Where(t => t.LineName == linename && t.MemberId == memberId).ToList();
        }

        // Get distinct line names from both tables for dropdown
        public async Task<List<string>> GetAllDistinctLineNamesAsync()
        {
            var finalLines = await _context.FinaltransactionData
                .Where(t => t.linename != null)
                .Select(t => t.linename!)
                .Distinct()
                .ToListAsync();

            var transactionLines = await _context.transactionData
                .Where(t => t.linename != null)
                .Select(t => t.linename!)
                .Distinct()
                .ToListAsync();

            return finalLines.Union(transactionLines).Distinct().ToList();
        }

    }

    // Model for line summary
    public class LineSummaryModel
    {
        public string LineName { get; set; } = string.Empty;
        public int TotalMembers { get; set; }
        public int TotalBags { get; set; }
        public int TotalGoldLeafWeight { get; set; }
        public int TotalNormalLeafWeight { get; set; }
        public int TotalWeight { get; set; }
    }

    // Unified view model for Transaction View (combines FinalTransaction and Transaction)
    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string? MemberId { get; set; }
        public string? Name { get; set; }
        public string? LineName { get; set; }
        public int BoxCount { get; set; }
        public int BagCount { get; set; }
        public int GoldLeafWeight { get; set; }
        public int NormalLeafWeight { get; set; }
        public int TotalWeight { get; set; }
        public string? Date { get; set; }
        public string Status { get; set; } = "Queue"; // "Completed" or "Queue"
    }
}
