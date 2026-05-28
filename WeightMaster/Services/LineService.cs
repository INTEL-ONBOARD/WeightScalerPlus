using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;

namespace WeightMaster.Services
{
    public class LineService
    {
        private readonly AppDbContext _context;

        public LineService(AppDbContext context)
        {
            _context = context;
        }

        // Map LineData to LineBlockModel (entity model for database)
        private LineBlockModel MapLineDataToEntity(LineData line)
        {
            return new LineBlockModel
            {
                LineId = line.LineId,
                LineName = line.LineName,
                LineMaster = line.LineMaster
            };
        }

        // Save line data to the database
        public async Task SaveLineDataAsync(List<LineData> lines)
        {
            var lineEntities = lines.Select(line => MapLineDataToEntity(line)).ToList();

            await _context.lineDbLog.AddRangeAsync(lineEntities);
            await _context.SaveChangesAsync();
        }

        // Replace existing line data with new data
        public async Task ReplaceLineDataAsync(List<LineData> lines)
        {
            _context.lineDbLog.RemoveRange(_context.lineDbLog);  // Clear existing records

            var lineEntities = lines.Select(line => MapLineDataToEntity(line)).ToList();

            await _context.lineDbLog.AddRangeAsync(lineEntities);
            await _context.SaveChangesAsync();
        }

        // Get total count of line records
        public async Task<int> GetLineCountAsync()
        {
            return await _context.lineDbLog.CountAsync();
        }

        public async Task<bool> HasMissingLineIdsAsync()
        {
            return await _context.lineDbLog.AnyAsync(line => line.LineId <= 0);
        }

        public async Task<string> GetLineIdByLineNameAsync(string lineName)
        {
            var matchingLine = await _context.lineDbLog
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.LineName == lineName);

            return matchingLine?.LineId > 0 ? matchingLine.LineId.ToString() : string.Empty;
        }

        // Retrieve line data from the database
        public async Task<List<LineBlockModel>> GetLineDataAsync()
        {
            return await _context.lineDbLog
                .Select(line => new LineBlockModel
                {
                    Id = line.Id,
                    LineId = line.LineId,
                    LineName = line.LineName,
                    LineMaster = line.LineMaster
                })
                .ToListAsync();
        }
    }
}
