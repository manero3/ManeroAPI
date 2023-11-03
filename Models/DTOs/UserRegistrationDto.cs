using System.ComponentModel.DataAnnotations;

namespace ManeroBackendAPI.Models.DTOs;

public class UserRegistrationDto
{
    [Required(ErrorMessage = "Email is required")]
    [DataType(DataType.EmailAddress)]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirm Password is required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password and Confirm Password must match")]
    public string ConfirmPassword { get; set; }

    public string? OAuthId { get; set; }
    public string? OAuthProvider { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

}
