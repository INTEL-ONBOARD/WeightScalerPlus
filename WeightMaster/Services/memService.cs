using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeightMaster.Config;
using WeightMaster.Models;

namespace WeightMaster.Services
{
    public class MemService
    {
        private readonly AppDbContext _context;

        public MemService(AppDbContext context)
        {
            _context = context;
        }

        // Map Members to memDbLog
        private memDbLog MapMemberToMemDbLog(Members member)
        {
            return new memDbLog
            {
                CustomMemberNum = member.CustomMemberNum,
                CustomPreMemberNum = member.CustomPreMemberNum,
                CustomNameWithInitials = member.CustomNameWithInitials ?? string.Empty,
                CellNumber = member.CellNumber
            };
        }

        // Save members to the database
        public async Task SaveMembersAsync(List<Members> members)
        {
            var memDbLogs = members.Select(member => MapMemberToMemDbLog(member)).ToList();

            await _context.memDbLog.AddRangeAsync(memDbLogs);
            await _context.SaveChangesAsync();
        }

        // Replace all members in the database
        public async Task ReplaceMembersAsync(List<Members> members)
        {
            _context.memDbLog.RemoveRange(_context.memDbLog);

            var memDbLogs = members.Select(member => MapMemberToMemDbLog(member)).ToList();

            await _context.memDbLog.AddRangeAsync(memDbLogs);
            await _context.SaveChangesAsync();
        }

        // Get the total count of members
        public async Task<int> GetMemberCountAsync()
        {
            return await _context.memDbLog.CountAsync();
        }

        // Check if a member exists based on custom member number
        public async Task<bool> MemberExistsAsync(string customMemberNum)
        {
            return await _context.memDbLog.AnyAsync(member => member.CustomMemberNum == customMemberNum);
        }

        // Get all custom names with initials
        public async Task<List<string>> GetAllCustomNamesWithInitialsAsync()
        {
            var initials = await _context.memDbLog
                .Select(m => m.CustomNameWithInitials)
                .ToListAsync();

            foreach (var name in initials)
            {
                Console.WriteLine("Member Initial: " + name);
            }

            return initials;
        }

        // Get the name with initials for a given custom member number
        public async Task<string> GetCustomNameWithInitialsAsync(string customMemberNum)
        {
            var member = await _context.memDbLog
                .FirstOrDefaultAsync(m => m.CustomMemberNum == customMemberNum);

            return member?.CustomNameWithInitials ?? "No initials found";
        }

        // Get the cell number for a given custom member number
        public async Task<string> GetCellNumberByCustomMemberNumAsync(string customMemberNum)
        {
            var member = await _context.memDbLog
                .FirstOrDefaultAsync(m => m.CustomMemberNum == customMemberNum);

            return member?.CellNumber ?? "";
        }
    }
}
