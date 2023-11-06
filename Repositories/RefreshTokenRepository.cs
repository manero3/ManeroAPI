using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Models;

namespace ManeroBackendAPI.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken, ApplicationDBContext>
{
    public RefreshTokenRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken)
    {
    
        return await this.CreateAsync(refreshToken);
    }
}

