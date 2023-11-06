using ManeroBackendAPI.Models;
using ManeroBackendAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace ManeroBackendAPI.Contexts
{
    public class ApplicationUser : IdentityUser<Guid>  // Notice the <Guid> generic parameter
    {
        // No need to redefine the Id property. It's already defined as Guid in the IdentityUser<Guid> base class.

        public string? FullName { get; set; }   // You might want to add a setter here if you want to modify it.
        public string? RefreshToken { get; set; }
        public string? OAuthProvider { get; set; }
        public string? OAuthId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();


    }


}
