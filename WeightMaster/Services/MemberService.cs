using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeightMaster.Services
{
    public class MemberService
    {
        private readonly AppDbContext _context;

        public MemberService(AppDbContext context)
        {
            _context = context;
        }

        // Map Member to MemberBlockModel
        private MemberBlockModel MapMemberToMemberBlockModel(Member member)
        {
            return new MemberBlockModel
            {
                Name = member.Name,
                CustomMemberNum = member.CustomMemberNum,
                CustomNameWithInitials = member.CustomNameWithInitials ?? string.Empty, // Handle potential null values
                CellNumber = member.CellNumber
            };
        }

        // Save members to the database
        public async Task SaveMembersAsync(List<Member> members)
        {
            // Map each member to a MemberBlockModel
            var memberBlockModels = members.Select(member => MapMemberToMemberBlockModel(member)).ToList();

            // Save the mapped members into the database
            await _context.MembersData.AddRangeAsync(memberBlockModels);
            await _context.SaveChangesAsync();
        }

        // Replace all members in the database
        public async Task ReplaceMembersAsync(List<Member> members)
        {
            // Step 1: Clean the table (delete all rows)
            _context.MembersData.RemoveRange(_context.MembersData); // This removes all records from the Members table.

            // Step 2: Map each member to a MemberBlockModel
            var memberBlockModels = members.Select(member => MapMemberToMemberBlockModel(member)).ToList();

            // Step 3: Save the mapped members into the database
            await _context.MembersData.AddRangeAsync(memberBlockModels); // Add the new data
            await _context.SaveChangesAsync(); // Commit changes to the database
        }

        // Get the total count of members
        public async Task<int> GetMemberCountAsync()
        {
            return await _context.MembersData.CountAsync();
        }

        // Check if a member exists based on a specific custom member number
        public async Task<bool> MemberExistsAsync(string customMemberNum)
        {
            return await _context.MembersData.AnyAsync(member => member.CustomMemberNum == customMemberNum);
        }

        // Log member activity (you can modify this to track specific actions)


        // Get all member names
        public async Task<List<string>> GetAllMemberNamesAsync()
        {
            // Use LINQ to query the database and select only the member names
            var memberNames = await _context.MembersData
                .Select(member => member.Name)
                .ToListAsync();

            foreach (var name in memberNames)
            {
                Console.WriteLine("Member Name: " + name);
            }

            return memberNames;
        }

        // Method to get the custom name with initials based on custom member number
        public async Task<string> GetCustomNameWithInitialsAsync(String customMemberNum)
        {
            // Use LINQ to query the database for the member with the given customMemberNum
            var member = await _context.MembersData
                .FirstOrDefaultAsync(m => m.CustomMemberNum == customMemberNum);

            // If the member is found, return their custom name with initials, otherwise return null or an empty string
            return member?.CustomNameWithInitials ?? "No name with initials found";
        }

        // Method to get the cell number based on custom member number
        public async Task<string> GetCellNumberByCustomMemberNumAsync(string customMemberNum)
        {
            var member = await _context.MembersData
                .FirstOrDefaultAsync(m => m.CustomMemberNum == customMemberNum);

            return member?.CellNumber ?? "";
        }



    }
}
