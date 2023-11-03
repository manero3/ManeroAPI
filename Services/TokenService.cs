using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Azure;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Authentication;
using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ManeroBackendAPI.Services;

public interface ITokenService
{
    Task<string> GetTokenAsync(string email, string password, bool isRememberMe);
    Task<string> RefreshTokenAsync(string accessToken, string refreshToken);
    Guid GetUserIdFromToken(string token); // Add this line
}

public class TokenService : ITokenService
{
    private readonly IUsersRepository _usersRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly string _securityKey = null!;


    public TokenService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUsersRepository usersRepository)
    {
       
            _userManager = userManager;
            _usersRepository = usersRepository;
            _signInManager = signInManager;
        _securityKey = "0Fv3pPuWTaPpBdGyzF2dUAxeIwxwMGIhUChMpQ/V6MA=";




    }

    public async Task<string> GetTokenAsync(string email,  string password, bool isRememberMe)
    {
        /// To generate a new key and encode it to base64:
        var hmac = new HMACSHA256();
        var key = Convert.ToBase64String(hmac.Key);
        Console.WriteLine(key);


        var response = new ServiceResponse<UserWithTokenResponse>();
        try
        {
            var user = await _usersRepository.GetUserByEmailAsync(email);
         
         
            var userByEmail = await _usersRepository.GetUserByEmailAsync(email);
         

            var userName = await _signInManager.UserManager.FindByEmailAsync(email);
          

            if (userName != null)
            {
                // Depending on your setup, you might need to use user.UserName instead of user.Email
                var result = await _signInManager.PasswordSignInAsync(userName, password, isRememberMe, false);

                if (result.Succeeded)
                {
                    // Assuming that the 'user' object has an 'Id' property of type Guid
                    var token = GenerateAuthToken(user.Email, user.Id); // Use the user's ID here
                    var refreshToken = GenerateRefreshToken();
                    user.RefreshToken = refreshToken;
                    await _userManager.UpdateAsync(user);
                    return token;
                }
                else
{
    var errorMessage = result.IsLockedOut ? "User account is locked out."
                    : result.IsNotAllowed ? "User is not allowed to login."
                    : result.RequiresTwoFactor ? "Login requires two-factor authentication."
                    : "Invalid login attempt.";
    Debug.WriteLine(errorMessage);
    response.Message = errorMessage;
}

            }
            else
            {
                response.Message = "User not found";
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = Enums.StatusCode.InternalServerError;
            response.Message = ex.Message;
            // Log the exception details here
        }
        return null; // Consider returning an appropriate error message or throw an exception
    }


    public async Task<string> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);
      

       

        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var emailClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        if (emailClaim == null || userIdClaim == null)
        {
            throw new Exception("Invalid token: email or user ID claim missing.");
        }

        var email = emailClaim.Value;
        var userIdString = userIdClaim.Value;
        Guid userId;

        if (!Guid.TryParse(userIdString, out userId))
        {
            throw new Exception("Invalid token: user ID claim is not a valid GUID.");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null || user.RefreshToken != refreshToken)
        {
            throw new Exception("Invalid refresh token.");
        }

        var newToken = GenerateAuthToken(email, userId); // Pass userId here
        user.RefreshToken = GenerateRefreshToken();
        await _userManager.UpdateAsync(user);

        return newToken;
    }
    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_securityKey))
        };
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = GetTokenValidationParameters();
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }

    private string GenerateAuthToken(string email, Guid userId) // Add Guid userId as a parameter
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_securityKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()) // Add this line
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }




    public Guid GetUserIdFromToken(string token)
    {
        var tokenValidationParameters = GetTokenValidationParameters();
        // Override the ValidateLifetime to true, so it checks if the token is not expired
        tokenValidationParameters.ValidateLifetime = true;

        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            foreach (var claim in principal.Claims)
            {
                Console.WriteLine($"Claim type: {claim.Type}, Claim value: {claim.Value}");
            }
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            else
            {
                throw new SecurityTokenException("Invalid token: UserID claim missing or invalid.");
            }
        }
        catch (SecurityTokenValidationException)
        {
            throw; // Token is invalid or expired
        }
        catch (Exception ex)
        {
            // Log the exception
            throw new Exception("An error occurred while validating the token.", ex);
        }
    }


    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}



