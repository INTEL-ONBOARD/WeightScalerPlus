using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeightMaster.Services
{
    public class PostService
    {
        private readonly AppDbContext _context;

        public PostService(AppDbContext context)
        {
            _context = context;
        }

        // Map to ensure decoupled cloning of model instances
        private GreenLeafPostModel MapPostToModel(GreenLeafPostModel post)
        {
            return new GreenLeafPostModel
            {
                Id = post.Id,
                LeafHandoverDate = post.LeafHandoverDate,
                Factory = post.Factory,
                TransportLineName = post.TransportLineName,
                TransportAgent = post.TransportAgent,
                LeafWeightOfficer = post.LeafWeightOfficer,
                Supervisor = post.Supervisor,
                MemberNumber = post.MemberNumber,
                PreMemberNumber = post.PreMemberNumber,
                BagCount = post.BagCount,
                BoxCount = post.BoxCount,
                RealWeight = post.RealWeight,
                TotalWeight = post.TotalWeight,
                NomalLeafWeight = post.NomalLeafWeight,
                GoldLeafWeight = post.GoldLeafWeight,
                Wathurata = post.Wathurata,
                Morapuwata = post.Morapuwata,
                Thambimata = post.Thambimata,
                Rejected = post.Rejected,
                BagWeight = post.BagWeight,
                BoxWeight = post.BoxWeight,
                FinalGreenLeafCount = post.FinalGreenLeafCount,
                FinalGoldLeafCount = post.FinalGoldLeafCount,
                CreatedUser = post.CreatedUser,
                UpdatedUser = post.UpdatedUser
            };
        }

        public async Task SavePostsAsync(List<GreenLeafPostModel> posts)
        {
            var postModels = posts.Select(p => MapPostToModel(p)).ToList();
            await _context.GreenLeafPosts.AddRangeAsync(postModels);
            await _context.SaveChangesAsync();
        }

        public async Task ReplacePostsAsync(List<GreenLeafPostModel> posts)
        {
            _context.GreenLeafPosts.RemoveRange(_context.GreenLeafPosts);
            var postModels = posts.Select(p => MapPostToModel(p)).ToList();
            await _context.GreenLeafPosts.AddRangeAsync(postModels);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetPostCountAsync()
        {
            return await _context.GreenLeafPosts.CountAsync();
        }

        public async Task<GreenLeafPostModel> GetPostByIdAsync(int id)
        {
            return await _context.GreenLeafPosts.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task DeletePostByIdAsync(int id)
        {
            var post = await GetPostByIdAsync(id);
            if (post != null)
            {
                _context.GreenLeafPosts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> AddPostAsync(GreenLeafPostModel newPost)
        {
            try
            {
                var postModel = MapPostToModel(newPost);
                await _context.GreenLeafPosts.AddAsync(postModel);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<GreenLeafPostModel>> GetPostsByDateAsync(string date)
        {
            return await _context.GreenLeafPosts
                .Where(p => p.LeafHandoverDate == date)
                .ToListAsync();
        }

        public async Task<List<GreenLeafPostModel>> GetPostsByTransportAgentAsync(string agent)
        {
            return await _context.GreenLeafPosts
                .Where(p => p.TransportAgent == agent)
                .ToListAsync();
        }

        public async Task<GreenLeafPostModel?> GetLatestPostAsync()
        {
            return await _context.GreenLeafPosts
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }


        public async Task UpdatePostAsync(int id, GreenLeafPostModel updatedPost)
        {
            var existingPost = await GetPostByIdAsync(id);
            if (existingPost != null)
            {
                // Update properties
                existingPost.LeafHandoverDate = updatedPost.LeafHandoverDate;
                existingPost.Factory = updatedPost.Factory;
                existingPost.TransportLineName = updatedPost.TransportLineName;
                existingPost.TransportAgent = updatedPost.TransportAgent;
                existingPost.LeafWeightOfficer = updatedPost.LeafWeightOfficer;
                existingPost.Supervisor = updatedPost.Supervisor;
                existingPost.MemberNumber = updatedPost.MemberNumber;
                existingPost.PreMemberNumber = updatedPost.PreMemberNumber;
                existingPost.BagCount = updatedPost.BagCount;
                existingPost.BoxCount = updatedPost.BoxCount;
                existingPost.RealWeight = updatedPost.RealWeight;
                existingPost.TotalWeight = updatedPost.TotalWeight;
                existingPost.NomalLeafWeight = updatedPost.NomalLeafWeight;
                existingPost.GoldLeafWeight = updatedPost.GoldLeafWeight;
                existingPost.Wathurata = updatedPost.Wathurata;
                existingPost.Morapuwata = updatedPost.Morapuwata;
                existingPost.Thambimata = updatedPost.Thambimata;
                existingPost.Rejected = updatedPost.Rejected;
                existingPost.BagWeight = updatedPost.BagWeight;
                existingPost.BoxWeight = updatedPost.BoxWeight;
                existingPost.FinalGreenLeafCount = updatedPost.FinalGreenLeafCount;
                existingPost.FinalGoldLeafCount = updatedPost.FinalGoldLeafCount;
                existingPost.CreatedUser = updatedPost.CreatedUser;
                existingPost.UpdatedUser = updatedPost.UpdatedUser;

                await _context.SaveChangesAsync();
            }
        }
    }
}
