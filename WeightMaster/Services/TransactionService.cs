using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeightMaster.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        // Map TransactionLogBlockModel to a view model or other relevant model
        private TransactionLogBlockModel MapTransactionToModel(TransactionLogBlockModel transaction)
        {
            return new TransactionLogBlockModel
            {
                LineName = transaction.LineName,
                TransportAgent = transaction.TransportAgent,
                Company = transaction.Company,
                LeafWeightOfficer = transaction.LeafWeightOfficer,
                Supervisor = transaction.Supervisor,
                BarcodeDetails = transaction.BarcodeDetails,
                NameWithInitials = transaction.NameWithInitials,
                PhoneNumber = transaction.PhoneNumber,
                Date = transaction.Date,
                BoxCount = transaction.BoxCount,
                BagCount = transaction.BagCount,
                MaximumNormalLeafWeight = transaction.MaximumNormalLeafWeight,
                TotalLeafWeight = transaction.TotalLeafWeight,
                ActualNormalLeafWeight = transaction.ActualNormalLeafWeight,
                TotalGoldLeafWeight = transaction.TotalGoldLeafWeight,
                Water = transaction.Water,
                Morapuwata = transaction.Morapuwata,
                Thambimata = transaction.Thambimata,
                Reject = transaction.Reject,
                BoxWeight = transaction.BoxWeight,
                FinalGreenLeafCount = transaction.FinalGreenLeafCount,
                FinalGoldLeafCount = transaction.FinalGoldLeafCount
            };
        }

        // Save transaction logs to the database
        public async Task SaveTransactionsAsync(List<TransactionLogBlockModel> transactions)
        {
            var transactionLogModels = transactions.Select(t => MapTransactionToModel(t)).ToList();
            await _context.transactionData.AddRangeAsync(transactionLogModels);
            await _context.SaveChangesAsync();
        }

        // Replace all transactions in the database
        public async Task ReplaceTransactionsAsync(List<TransactionLogBlockModel> transactions)
        {
            // Step 1: Clean the table (delete all rows)
            _context.transactionData.RemoveRange(_context.transactionData);

            // Step 2: Map and save new transactions
            var transactionLogModels = transactions.Select(t => MapTransactionToModel(t)).ToList();
            await _context.transactionData.AddRangeAsync(transactionLogModels);
            await _context.SaveChangesAsync();
        }

        // Get the total count of transactions
        public async Task<int> GetTransactionCountAsync()
        {
            return await _context.transactionData.CountAsync();
        }

        // Check if a transaction exists based on LineName and Date
        public async Task<bool> TransactionExistsAsync(string lineName, DateTime date)
        {
            return await _context.transactionData.AnyAsync(t => t.LineName == lineName && t.Date == date);
        }

        // Get all transactions by a specific TransportAgent
        public async Task<List<TransactionLogBlockModel>> GetTransactionsByTransportAgentAsync(string transportAgent)
        {
            return await _context.transactionData
                .Where(t => t.TransportAgent == transportAgent)
                .ToListAsync();
        }

        // Get a specific transaction by ID
        public async Task<TransactionLogBlockModel> GetTransactionByIdAsync(int id)
        {
            return await _context.transactionData.FirstOrDefaultAsync(t => t.Id == id);
        }

        // Delete a transaction by ID
        public async Task DeleteTransactionByIdAsync(int id)
        {
            var transaction = await GetTransactionByIdAsync(id);
            if (transaction != null)
            {
                _context.transactionData.Remove(transaction);
                await _context.SaveChangesAsync();
            }
        }
        // Update a specific transaction's details
        public async Task SetTransactionAsync(int id, TransactionLogBlockModel updatedTransaction)
        {
            var existingTransaction = await GetTransactionByIdAsync(id);

            if (existingTransaction != null)
            {
                // Update the transaction fields with the provided data
                existingTransaction.LineName = updatedTransaction.LineName;
                existingTransaction.TransportAgent = updatedTransaction.TransportAgent;
                existingTransaction.Company = updatedTransaction.Company;
                existingTransaction.LeafWeightOfficer = updatedTransaction.LeafWeightOfficer;
                existingTransaction.Supervisor = updatedTransaction.Supervisor;
                existingTransaction.BarcodeDetails = updatedTransaction.BarcodeDetails;
                existingTransaction.NameWithInitials = updatedTransaction.NameWithInitials;
                existingTransaction.PhoneNumber = updatedTransaction.PhoneNumber;
                existingTransaction.Date = updatedTransaction.Date;
                existingTransaction.BoxCount = updatedTransaction.BoxCount;
                existingTransaction.BagCount = updatedTransaction.BagCount;
                existingTransaction.MaximumNormalLeafWeight = updatedTransaction.MaximumNormalLeafWeight;
                existingTransaction.TotalLeafWeight = updatedTransaction.TotalLeafWeight;
                existingTransaction.ActualNormalLeafWeight = updatedTransaction.ActualNormalLeafWeight;
                existingTransaction.TotalGoldLeafWeight = updatedTransaction.TotalGoldLeafWeight;
                existingTransaction.Water = updatedTransaction.Water;
                existingTransaction.Morapuwata = updatedTransaction.Morapuwata;
                existingTransaction.Thambimata = updatedTransaction.Thambimata;
                existingTransaction.Reject = updatedTransaction.Reject;
                existingTransaction.BoxWeight = updatedTransaction.BoxWeight;
                existingTransaction.FinalGreenLeafCount = updatedTransaction.FinalGreenLeafCount;
                existingTransaction.FinalGoldLeafCount = updatedTransaction.FinalGoldLeafCount;

                // Save the updated transaction to the database
                await _context.SaveChangesAsync();
            }
        }

        // Add a new transaction
        public async Task<bool> AddTransactionAsync(TransactionLogBlockModel newTransaction)
        {
            try
            {
                var transactionLogModel = MapTransactionToModel(newTransaction);

                // Add the transaction to the database
                await _context.transactionData.AddAsync(transactionLogModel);

                // Save changes to the database
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }

        }



    }
}
