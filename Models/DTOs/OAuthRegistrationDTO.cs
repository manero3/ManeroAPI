using System.ComponentModel.DataAnnotations;

namespace ManeroBackendAPI.Models.DTOs
{
    public class OAuthRegistrationDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OAuth ID is required")]
        public string OAuthId { get; set; }

        [Required(ErrorMessage = "OAuth Provider is required")]
        public string OAuthProvider { get; set; }

        public string Password { get; set; }

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    }
}
