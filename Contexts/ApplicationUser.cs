using ManeroBackendAPI.Models;
using ManeroBackendAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace ManeroBackendAPI.Contexts
{
    public class ApplicationUser : IdentityUser<Guid>  // Notice the <Guid> generic parameter
    {
      

        public string? FullName { get; set; }  
        public string? RefreshToken { get; set; }
        public string? OAuthProvider { get; set; }
        public string? OAuthId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();


    }


}
