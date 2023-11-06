using System.Diagnostics;
using ManeroBackendAPI.Models.Entities;
using ManeroBackendAPI.Contexts;
using Microsoft.EntityFrameworkCore;
using ManeroBackendAPI.Models;

namespace ManeroBackendAPI.Repositories;

public interface IUsersRepository : IRepository<ApplicationUser, ApplicationDBContext>
{
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task SaveRefreshToken(RefreshToken refreshToken);
}


public class UsersRepository : Repository<ApplicationUser, ApplicationDBContext>, IUsersRepository
{


    public UsersRepository(ApplicationDBContext context) : base(context)
    {

    }
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.Trim() == email.Trim());
        Debug.WriteLine($"Searched for email: {email}. Found: {user?.Email ?? "No user found"}");
        return user;
    }

    public async Task SaveRefreshToken(RefreshToken refreshToken) // Async method
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
    }


}
