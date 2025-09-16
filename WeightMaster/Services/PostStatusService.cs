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
            var existing = await _context.PostStatus.FirstOrDefaultAsync(ps => ps.PostId == postId);
            if (existing != null)
            {
                existing.Status = newStatus;
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

            MessageBox.Show("PostStatus found: " + (postStatus != null ? postStatus.PostId.ToString() : "null"));

            if (postStatus != null)
            {
                MessageBox.Show("Fetching GreenLeafPost for PostId: " + postStatus.PostId);
                return await _context.GreenLeafPosts
                    .FirstOrDefaultAsync(post => post.id == postStatus.PostId);
            }

            return null;
        }

    }
}
