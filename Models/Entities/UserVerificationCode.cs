using ManeroBackendAPI.Contexts;

namespace ManeroBackendAPI.Models.Entities;


public enum VerificationType
{
    SignUp,
    TwoFactor,
    PasswordReset

}
public class UserVerificationCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Code { get; set; } = null!;
    public DateTime ExpiryDate { get; set; }
    public VerificationType Type { get; set; }


    public virtual ApplicationUser User { get; set; } = null!;
}
