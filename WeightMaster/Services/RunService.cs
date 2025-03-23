using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeightMaster.Services
{
    public class RunService
    {
        private readonly AppDbContext _context;

        public RunService(AppDbContext context)
        {
            _context = context;
        }


        // Map RunLog to RunLog model
        private RunLog MapRunLogToModel(RunLog runLog)
        {
            return new RunLog
            {
                Status = runLog.Status,
                Date = runLog.Date,
                Transaction = runLog.Transaction,
                FinalTransaction = runLog.FinalTransaction
            };
        }

        // Save RunLogs to the database
        public async Task SaveRunLogsAsync(List<RunLog> runLogs)
        {
            var runLogModels = runLogs.Select(runLog => MapRunLogToModel(runLog)).ToList();
            await _context.RunLog.AddRangeAsync(runLogModels);
            await _context.SaveChangesAsync();
        }

        // Replace all RunLogs in the database
        public async Task ReplaceRunLogsAsync(List<RunLog> runLogs)
        {
            // Step 1: Clean the table (delete all rows)
            _context.RunLog.RemoveRange(_context.RunLog);

            // Step 2: Map and save new RunLogs
            var runLogModels = runLogs.Select(runLog => MapRunLogToModel(runLog)).ToList();
            await _context.RunLog.AddRangeAsync(runLogModels);
            await _context.SaveChangesAsync();
        }

        // Get the total count of RunLogs
        public async Task<int> GetRunLogCountAsync()
        {
            return await _context.RunLog.CountAsync();
        }

        // Check if a RunLog exists based on TransactionId and Date
        public async Task<bool> RunLogExistsAsync(int transactionId, DateTime date)
        {
            return await _context.RunLog.AnyAsync(runLog => runLog.Transaction.Id == transactionId && runLog.Date == date);
        }

        // Get all RunLogs by a specific TransactionId
        public async Task<List<RunLog>> GetRunLogsByTransactionIdAsync(int transactionId)
        {
            return await _context.RunLog
                .Where(runLog => runLog.Transaction.Id == transactionId)
                .ToListAsync();
        }

        // Get a specific RunLog by ID
        public async Task<RunLog> GetRunLogByIdAsync(int id)
        {
            return await _context.RunLog.FirstOrDefaultAsync(runLog => runLog.Id == id);
        }

        // Delete a RunLog by ID
        public async Task DeleteRunLogByIdAsync(int id)
        {
            var runLog = await GetRunLogByIdAsync(id);
            if (runLog != null)
            {
                _context.RunLog.Remove(runLog);
                await _context.SaveChangesAsync();
            }
        }

        // Add a new RunLog
        public async Task AddRunLogAsync(RunLog newRunLog)
        {
            var runLogModel = MapRunLogToModel(newRunLog);
            await _context.RunLog.AddAsync(runLogModel);
            await _context.SaveChangesAsync();
        }

        // Update an existing RunLog
        public async Task UpdateRunLogAsync(RunLog updatedRunLog)
        {
            var runLog = await GetRunLogByIdAsync(updatedRunLog.Id);
            if (runLog != null)
            {
                runLog.Status = updatedRunLog.Status;
                runLog.Date = updatedRunLog.Date;
                runLog.Transaction = updatedRunLog.Transaction;
                runLog.FinalTransaction = updatedRunLog.FinalTransaction;

                await _context.SaveChangesAsync();
            }
        }

        // Get the most recent updated RunLog
        public async Task<RunLog> GetMostRecentRunLogAsync()
        {
            return await _context.RunLog
                .OrderByDescending(r => r.LastUpdated)
                .FirstOrDefaultAsync(); // Fetch the row with the most recent LastUpdated time
        }

        // 1. Get the count of rows where Status is 0
        // 1. Get the count of rows where Status is false
        public async Task<int> GetCountOfStatusFalseAsync()
        {
            return await _context.RunLog.CountAsync(runLog => runLog.Status == false);
        }

        // 2. Get the latest RunLog record where Status is 0
        // 2. Get the latest RunLog record where Status is false
        public async Task<RunLog> GetLatestRunLogWithStatusFalseAsync()
        {
            // Count how many rows match the condition
            var count = await _context.RunLog
                .Where(runLog => runLog.Status == false)
                .CountAsync();

            // Log the count value (you can replace this with your preferred logging mechanism)
            Console.WriteLine($"Number of RunLogs with Status = false: {count}");

            // After logging, proceed to fetch the first result as usual
            var latestRunLog = await _context.RunLog
                .Where(runLog => runLog.Status == false)
                .OrderByDescending(runLog => runLog.Date)
                .FirstOrDefaultAsync();

            return latestRunLog;


        }


        // 3. Update the Status of the passed RunLog to true
        public async Task UpdateRunLogStatusToTrueAsync(RunLog runLog)
        {
            var existingRunLog = await GetRunLogByIdAsync(runLog.Id); // Assuming you're identifying by Id

            if (existingRunLog != null)
            {
                existingRunLog.Status = true; // Set the status to true
                await _context.SaveChangesAsync(); // Save changes to the database
            }
        }


    }
}
