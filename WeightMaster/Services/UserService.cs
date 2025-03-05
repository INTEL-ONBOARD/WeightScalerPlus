using Microsoft.EntityFrameworkCore;
using WeightMaster.Config;
using WeightMaster.Models;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    // Map User to UserBlockModel
    private UserBlockModel MapUserToUserBlockModel(User user)
    {
        return new UserBlockModel
        {
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Roles = user.Roles,
            ApiKey = user.ApiKey ?? "",
            ApiSecret = user.ApiSecret ?? ""
        };
    }

    // Save users to the database
    public async Task SaveUsersAsync(List<User> users)
    {
        // Map each user to a UserBlockModel
        var userBlockModels = users.Select(user => MapUserToUserBlockModel(user)).ToList();

        // Save the mapped users into the database  
        await _context.Users.AddRangeAsync(userBlockModels);
        await _context.SaveChangesAsync();
    }
    public async Task ReplaceUsersAsync(List<User> users)
    {
        // Step 1: Clean the table (delete all rows)
        _context.Users.RemoveRange(_context.Users);  // This removes all records from the Users table.

        // Step 2: Map each user to a UserBlockModel
        var userBlockModels = users.Select(user => MapUserToUserBlockModel(user)).ToList();

        // Step 3: Save the mapped users into the database
        await _context.Users.AddRangeAsync(userBlockModels);  // Add the new data
        await _context.SaveChangesAsync();  // Commit changes to the database
    }

    public async Task<int> GetUserCountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(user => user.Email == email);
    }
    public async Task<bool> LogUserLogin(string email, string password)
    {
        // Await the result of EmailExistsAsync
        if (await EmailExistsAsync(email))
        {
            var userLogin = new UserLoginModel
            {
                Email = email,
                Password = password, // Password should ideally be hashed
                Status = true,
                LoginDateTime = DateTime.Now // Capture current date and time
            };

            await _context.UserLogins.AddAsync(userLogin); // AddAsync for async operations
            await _context.SaveChangesAsync(); // SaveChangesAsync for async save to the database

            return true;
        }
        else
        {
            var userLogin = new UserLoginModel
            {
                Email = email,
                Password = password, // Password should ideally be hashed
                Status = false,
                LoginDateTime = DateTime.Now // Capture current date and time
            };

            await _context.UserLogins.AddAsync(userLogin); // AddAsync for async operations
            await _context.SaveChangesAsync();
            return false;
        }
    }


}
