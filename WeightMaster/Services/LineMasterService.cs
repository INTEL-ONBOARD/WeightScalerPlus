using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;

namespace WeightMaster.Services
{
    public class LineMasterService
    {
        private readonly AppDbContext _context;

        public LineMasterService(AppDbContext context)
        {
            _context = context;
        }

        // Map LineMaster to LineMasterEntityModel (assuming you have an entity for database storage)
        private LineMasterBlockModel MapLineMasterToEntity(LineMaster lineMaster)
        {
            return new LineMasterBlockModel
            {
                LineName = lineMaster.LineName,
                LineMaster = lineMaster.LineMasterName
            };
        }

        // Save line master data to the database
        public async Task SaveLineMasterDataAsync(List<LineMaster> lineMasters)
        {
            // Map each LineMaster to a LineMasterEntityModel
            var lineMasterEntities = lineMasters.Select(lineMaster => MapLineMasterToEntity(lineMaster)).ToList();

            // Save the mapped LineMaster data into the database  
            await _context.lineMasterData.AddRangeAsync(lineMasterEntities);
            await _context.SaveChangesAsync();
        }

        // Replace the existing line master data with new data
        public async Task ReplaceLineMasterDataAsync(List<LineMaster> lineMasters)
        {
            // Step 1: Clean the table (delete all rows)
            _context.lineMasterData.RemoveRange(_context.lineMasterData);  // This removes all records from the LineMasterData table.
            System.Diagnostics.Debug.WriteLine("> Data restoring");
            // Step 2: Map each LineMaster to a LineMasterEntityModel
            var lineMasterEntities = lineMasters.Select(lineMaster => MapLineMasterToEntity(lineMaster)).ToList();

            // Step 3: Save the mapped line master data into the database
            await _context.lineMasterData.AddRangeAsync(lineMasterEntities);  // Add the new data
            await _context.SaveChangesAsync();  // Commit changes to the database
        }

        // Get the count of line master records in the database
        public async Task<int> GetLineMasterCountAsync()
        {
            return await _context.lineMasterData.CountAsync();
        }

        // Retrieve line master data from the database
        public async Task<List<LineMasterBlockModel>> GetLineMasterDataAsync()
        {
            // Query the database and retrieve the data
            var lineMasterEntities = await _context.lineMasterData
                                                    .Select(entity => new LineMasterBlockModel
                                                    {
                                                        id = entity.id,
                                                        LineName = entity.LineName,
                                                        LineMaster = entity.LineMaster
                                                    })
                                                    .ToListAsync();
            

            return lineMasterEntities;
        }

    }
}
