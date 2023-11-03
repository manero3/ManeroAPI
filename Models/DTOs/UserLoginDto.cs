using System.ComponentModel.DataAnnotations;

namespace ManeroBackendAPI.Models.DTOs;

public class UserLoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [DataType(DataType.EmailAddress)]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public string? OAuthId { get; set; }
    public string? OAuthProvider { get; set; }

    public bool? IsRememberMe { get; set; }

    // Add these properties for JWT and refreshToken
    public string? JwtToken { get; set; }
    public string? RefreshToken { get; set; }
}
