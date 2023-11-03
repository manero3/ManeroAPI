using Google.Apis.Auth;

namespace ManeroBackendAPI.Authentication;

public class GoogleUser
{
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = null!;
    public string PictureUrl { get; set; } = null!;
    public string Locale { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string GivenName { get; set; } = null!;
}

