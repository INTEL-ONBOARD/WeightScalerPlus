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

    }
}
