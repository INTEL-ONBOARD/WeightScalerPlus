using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeightMaster.Config;
using WeightMaster.Models;
using System.Windows;

namespace WeightMaster.Services
{
    public class PostStatusService
    {
        private readonly AppDbContext _context;

        public PostStatusService(AppDbContext context)
        {
            _context = context;
        }

        // Map method (not really needed here but added for consistency)
        private PostStatusModel MapToModel(PostStatusModel status)
        {
            return new PostStatusModel
            {
                PostId = status.PostId,
                Status = status.Status
            };
        }

        // Save a list of PostStatus records
        public async Task SavePostStatusesAsync(List<PostStatusModel> statuses)
        {
            var models = statuses.Select(MapToModel).ToList();
            await _context.PostStatus.AddRangeAsync(models);
            await _context.SaveChangesAsync();
        }

        // Replace all PostStatus entries
        public async Task ReplacePostStatusesAsync(List<PostStatusModel> statuses)
        {
            _context.PostStatus.RemoveRange(_context.PostStatus);

            var models = statuses.Select(MapToModel).ToList();
            await _context.PostStatus.AddRangeAsync(models);
            await _context.SaveChangesAsync();
        }

        // Get count of PostStatus records
        public async Task<int> GetPostStatusCountAsync()
        {
            return await _context.PostStatus.CountAsync();
        }

        // Check if a status entry exists for a given PostId
        public async Task<bool> PostStatusExistsAsync(int postId)
        {
            return await _context.PostStatus.AnyAsync(ps => ps.PostId == postId);
        }

        // Get all statuses by a specific PostId
        public async Task<List<PostStatusModel>> GetStatusesByPostIdAsync(int postId)
        {
            return await _context.PostStatus
                .Where(ps => ps.PostId == postId)
                .ToListAsync();
        }

        // Get a specific PostStatus by Id
        public async Task<PostStatusModel?> GetPostStatusByIdAsync(int id)
        {
            return await _context.PostStatus.FirstOrDefaultAsync(ps => ps.Id == id);
        }

        // Delete a PostStatus by Id
        public async Task DeletePostStatusByIdAsync(int id)
        {
            var status = await GetPostStatusByIdAsync(id);
            if (status != null)
            {
                _context.PostStatus.Remove(status);
                await _context.SaveChangesAsync();
            }
        }

        // Add a new PostStatus
        public async Task AddPostStatusAsync(PostStatusModel newStatus)
        {
            var model = MapToModel(newStatus);
            await _context.PostStatus.AddAsync(model);
            await _context.SaveChangesAsync();
        }

        // Update an existing PostStatus
        public async Task UpdatePostStatusAsync(PostStatusModel updatedStatus)
        {
            var existing = await GetPostStatusByIdAsync(updatedStatus.Id);
            if (existing != null)
            {
                existing.PostId = updatedStatus.PostId;
                existing.Status = updatedStatus.Status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateStatusByPostIdAsync(int postId, bool newStatus)
        {
            // Update ALL rows for this PostId, not just the first. If duplicate PostStatus rows
            // exist for one post, flipping only the first leaves the others Status=false, which
            // makes cloudSync re-fetch the same post forever (stuck on "Cloud syncing...").
            var rows = await _context.PostStatus.Where(ps => ps.PostId == postId).ToListAsync();
            if (rows.Count > 0)
            {
                foreach (var row in rows)
                    row.Status = newStatus;
                await _context.SaveChangesAsync();
            }
        }


        // Get latest PostStatus with specific Status
        public async Task<PostStatusModel?> GetLatestPostStatusByStatusAsync(bool status)
        {
            return await _context.PostStatus
                .Where(ps => ps.Status == status)
                .OrderByDescending(ps => ps.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AnyPostStatusIsFalseAsync()
        {
            return await _context.PostStatus.AnyAsync(ps => ps.Status == false);
        }

        public async Task<GreenLeafPostModel?> GetFirstGreenLeafPostWithStatusFalseAsync()
        {
            var postStatus = await _context.PostStatus
                .Where(ps => ps.Status == false)
                .OrderBy(ps => ps.Id)
                .FirstOrDefaultAsync();

            if (postStatus != null)
            {
               
                return await _context.GreenLeafPosts
                    .FirstOrDefaultAsync(post => post.id == postStatus.PostId);
            }

            return null;
        }

        // Self-heal: mark every Status=false PostStatus whose GreenLeafPost no longer exists as
        // done, so orphaned rows can never block cloudSync. Returns the number of rows fixed.
        public async Task<int> MarkOrphanedStatusesDoneAsync()
        {
            var orphans = await _context.PostStatus
                .Where(ps => ps.Status == false
                             && !_context.GreenLeafPosts.Any(gp => gp.id == ps.PostId))
                .ToListAsync();

            foreach (var orphan in orphans)
                orphan.Status = true;

            if (orphans.Count > 0)
                await _context.SaveChangesAsync();

            return orphans.Count;
        }

        // Number of distinct posts still waiting to sync (Status=false) that still have a matching
        // GreenLeafPost. Orphaned status rows are excluded so they don't skew the progress total.
        public async Task<int> GetPendingRealPostCountAsync()
        {
            return await _context.PostStatus
                .Where(ps => ps.Status == false
                             && _context.GreenLeafPosts.Any(gp => gp.id == ps.PostId))
                .Select(ps => ps.PostId)
                .Distinct()
                .CountAsync();
        }

    }
}
