using ManeroBackendAPI.Models.Entities;
using ManeroBackendAPI.Contexts;

namespace ManeroBackendAPI.Authentication;


public class UserWithTokenResponse
{
    public ApplicationUser User { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}
