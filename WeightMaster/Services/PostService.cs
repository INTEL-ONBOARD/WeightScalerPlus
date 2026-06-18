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

        private GreenLeafPostModel MapPostToModel(GreenLeafPostModel post)
        {
            return new GreenLeafPostModel
            {
                id = post.id,
                leaf_handover_date = post.leaf_handover_date,
                factory = post.factory,
                line_id = post.line_id,
                transportlinename = post.transportlinename,
                transportagent = post.transportagent,
                leaf_weight_officer = post.leaf_weight_officer,
                supervisor = post.supervisor,
                membernumber = post.membernumber,
                premembernumber = post.premembernumber,
                bag_count = post.bag_count,
                box_count = post.box_count,
                real_weight = post.real_weight,
                total_weight = post.total_weight,
                nomal_leaf_weight = post.nomal_leaf_weight,
                gold_leaf_weight = post.gold_leaf_weight,
                wathurata = post.wathurata,
                morapuwata = post.morapuwata,
                thambimata = post.thambimata,
                rejected = post.rejected,
                bag_weight = post.bag_weight,
                box_weight = post.box_weight,
                final_green_leaf_count = post.final_green_leaf_count,
                final_gold_leaf_count = post.final_gold_leaf_count,
                created_user = post.created_user,
                updated_user = post.updated_user
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
            return await _context.GreenLeafPosts.FirstOrDefaultAsync(p => p.id == id);
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

                var postStatus = new PostStatusModel
                {
                    PostId = postModel.id,
                    Status = false
                };

                await _context.PostStatus.AddAsync(postStatus);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding post and status: {ex.Message}");
                return false;
            }
        }

        public async Task<List<GreenLeafPostModel>> GetPostsByDateAsync(string date)
        {
            return await _context.GreenLeafPosts
                .Where(p => p.leaf_handover_date == date)
                .ToListAsync();
        }

        public async Task<List<GreenLeafPostModel>> GetPostsByTransportAgentAsync(string agent)
        {
            return await _context.GreenLeafPosts
                .Where(p => p.transportagent == agent)
                .ToListAsync();
        }

        public async Task<GreenLeafPostModel?> GetLatestPostAsync()
        {
            return await _context.GreenLeafPosts
                .OrderByDescending(p => p.id)
                .FirstOrDefaultAsync();
        }

        public async Task UpdatePostAsync(int id, GreenLeafPostModel updatedPost)
        {
            var existingPost = await GetPostByIdAsync(id);
            if (existingPost != null)
            {
                existingPost.leaf_handover_date = updatedPost.leaf_handover_date;
                existingPost.factory = updatedPost.factory;
                existingPost.line_id = updatedPost.line_id;
                existingPost.transportlinename = updatedPost.transportlinename;
                existingPost.transportagent = updatedPost.transportagent;
                existingPost.leaf_weight_officer = updatedPost.leaf_weight_officer;
                existingPost.supervisor = updatedPost.supervisor;
                existingPost.membernumber = updatedPost.membernumber;
                existingPost.premembernumber = updatedPost.premembernumber;
                existingPost.bag_count = updatedPost.bag_count;
                existingPost.box_count = updatedPost.box_count;
                existingPost.real_weight = updatedPost.real_weight;
                existingPost.total_weight = updatedPost.total_weight;
                existingPost.nomal_leaf_weight = updatedPost.nomal_leaf_weight;
                existingPost.gold_leaf_weight = updatedPost.gold_leaf_weight;
                existingPost.wathurata = updatedPost.wathurata;
                existingPost.morapuwata = updatedPost.morapuwata;
                existingPost.thambimata = updatedPost.thambimata;
                existingPost.rejected = updatedPost.rejected;
                existingPost.bag_weight = updatedPost.bag_weight;
                existingPost.box_weight = updatedPost.box_weight;
                existingPost.final_green_leaf_count = updatedPost.final_green_leaf_count;
                existingPost.final_gold_leaf_count = updatedPost.final_gold_leaf_count;
                existingPost.created_user = updatedPost.created_user;
                existingPost.updated_user = updatedPost.updated_user;

                var existingStatus = await _context.PostStatus.FirstOrDefaultAsync(status => status.PostId == existingPost.id);
                if (existingStatus != null)
                {
                    existingStatus.Status = false;
                }
                else
                {
                    await _context.PostStatus.AddAsync(new PostStatusModel
                    {
                        PostId = existingPost.id,
                        Status = false
                    });
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<GreenLeafPostModel>> GetPostsByMemberAndDateAsync(string memberNumber, string handoverDate)
        {
            return await _context.GreenLeafPosts
                .Where(p => p.membernumber == memberNumber && p.leaf_handover_date == handoverDate)
                .ToListAsync();
        }

        public async Task<GreenLeafPostModel?> GetPostByMemberAndDateAsyncSingle(string memberNumber, string handoverDate)
        {
            return await _context.GreenLeafPosts
                .FirstOrDefaultAsync(p => p.membernumber == memberNumber && p.leaf_handover_date == handoverDate);
        }

        // Line-aware single lookup. A member can hand over on more than one line in a day (one row per
        // line). Resolving by member + date alone always returns the first line's row, so Station 2 would
        // overwrite that row's line name and bag weight and leave the other line's row at bag_weight = 0.
        // Keying on transportlinename (the line being weighed) targets the correct row for that line.
        public async Task<GreenLeafPostModel?> GetPostByMemberDateAndLineAsyncSingle(string memberNumber, string handoverDate, string line)
        {
            return await _context.GreenLeafPosts
                .FirstOrDefaultAsync(p => p.membernumber == memberNumber
                                       && p.leaf_handover_date == handoverDate
                                       && p.transportlinename == line);
        }

        public async Task<List<GreenLeafPostModel>> GetAllPostsByFiltering(string memberNumber, string line)
        {
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            return await _context.GreenLeafPosts.Where(P => P.membernumber == memberNumber && P.transportlinename == line && P.leaf_handover_date == todayDate ).ToListAsync();
        }

    }
}
