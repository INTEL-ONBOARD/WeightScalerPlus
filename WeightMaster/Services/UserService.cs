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
}
