using ManeroBackendAPI.Contexts;

namespace ManeroBackendAPI.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    public bool IsRevoked { get; set; }
    public Guid UserId { get; set; } 
    public ApplicationUser User { get; set; } 
    public bool RememberMe { get; set; }
}

