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
using ManeroBackendAPI.Controllers;
using Microsoft.IdentityModel.Logging;

namespace ManeroBackendAPI.Services;


public interface ITokenService
{
    Task<UserWithTokenResponse> GetTokenAsync(string email, string password, bool rememberme);
    Task<string> RefreshTokenAsync(string accessToken, string refreshToken);
    Guid GetUserIdFromToken(string token);
    Task<string> CreateTokenAsync(string email, Guid userId);

}

public class TokenService : ITokenService
{
    private readonly IUsersRepository _usersRepository;
    private readonly RefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenValidationService _tokenValidationService;
    private readonly string _securityKey = null!;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;
    private string keyId = "4b623c772ff94971e1b1bb0723b2a0cb";

    public TokenService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IUsersRepository usersRepository, IConfiguration configuration, ILogger<AccountController> logger, ITokenValidationService tokenValidationService, RefreshTokenRepository refreshTokenRepository)
    {

        _userManager = userManager;
        _usersRepository = usersRepository;
        _signInManager = signInManager;
        _securityKey = "4b623c772ff94971e1b1bb0723b2a0cb";
        _configuration = configuration;
        _logger = logger;
        _tokenValidationService = tokenValidationService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<UserWithTokenResponse> GetTokenAsync(string email, string password, bool rememberme)
    {



        var response = new ServiceResponse<UserWithTokenResponse>();
        try
        {
            var user = await _usersRepository.GetUserByEmailAsync(email);

            var userName = await _signInManager.UserManager.FindByEmailAsync(user.Email);


            if (userName != null)
            {

                var result = await _signInManager.PasswordSignInAsync(userName, password, rememberme, false);

                if (result.Succeeded)
                {
                    var token = GenerateAuthToken(user!.Email!, user.Id); // Generate the token
                                                                          // Generate the refresh token string and create the RefreshToken entity
                    var refreshTokenString = GenerateRefreshToken(user.Email, rememberme);
                    // Create the refresh token entity
                    var refreshTokenEntity = new RefreshToken
                    {
                        Token = refreshTokenString.ToString()!,
                        Expires = DateTime.UtcNow.AddDays(rememberme ? 30 : 7),
                        Created = DateTime.UtcNow,
                        UserId = user.Id,
                        RememberMe = rememberme
                    };

                    // Add the refresh token entity to the database using RefreshTokenRepository
                    await _refreshTokenRepository.AddRefreshTokenAsync(refreshTokenEntity);
                    // Immediately validate the token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var validationParameters = _tokenValidationService.GetTokenValidationParameters();

                    try
                    {
                        // This will throw an exception if the token is invalid
                        tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                    }
                    catch (SecurityTokenException ex)
                    {
                        // Handle the case where the token is invalid
                        Console.WriteLine($"Token validation error: {ex.Message}");
                        // Depending on your error handling, either throw, log the error, or return a failure response
                        throw;
                    }

                    // If the token is valid, then you can proceed to issue it to the client

                    await _userManager.UpdateAsync(user);

                    return new UserWithTokenResponse
                    {
                        User = user, // Assuming you want to send the user info back as well
                        Token = token, // This should match the property name in your UserWithTokenResponse class
                        RefreshToken = refreshTokenString.ToString()! // Send the refresh token string to the client
                    };

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

        if (user == null)
        {
            throw new Exception("User does not exist.");
        }

        // Find the matching refresh token in the user's collection of refresh tokens.
        var refreshTokenEntity = user.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshToken);

        if (refreshTokenEntity == null || refreshTokenEntity.IsRevoked)
        {
            throw new Exception("Invalid or revoked refresh token.");
        }

        // Now you can access the RememberMe property from the refreshTokenEntity
        var rememberMe = refreshTokenEntity.RememberMe;
        var newToken = GenerateAuthToken(email, userId); // Pass userId here

        // Generate a new refresh token and update the refresh token entity as needed
        var newRefreshTokenString = GenerateRefreshToken(user.Email!, rememberMe);
        refreshTokenEntity.Token = newRefreshTokenString.ToString()!; // Update the token string
        refreshTokenEntity.Expires = DateTime.UtcNow.AddDays(rememberMe ? 30 : 7); // Set new expiry

        // No need to set user.RefreshToken as it is a collection now
        // user.RefreshToken = newRefreshTokenString; // This line should be removed

        // Update the refresh token entity in the database
        await _usersRepository.SaveRefreshToken(refreshTokenEntity);


        return newToken;
    }

    public Task<string> CreateTokenAsync(string email, Guid userId)
    {
        // Since GenerateAuthToken is not an asynchronous method, you don't need to await it.
        // Instead, you can directly return the Task.FromResult to wrap the result into a Task.
        return Task.FromResult(GenerateAuthToken(email, userId));
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = _tokenValidationService.GetTokenValidationParameters();
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
    private string GenerateKeyIdentifier()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            var randomBytes = new byte[16]; // For a 128-bit key identifier
            rng.GetBytes(randomBytes);
            var keyId = BitConverter.ToString(randomBytes).Replace("-", "").ToLower();

            // Log the value with _logger
            _logger.LogInformation("Generated Key Identifier: {KeyId}", keyId);

            return keyId;
        }
    }

    private string GenerateAuthToken(string email, Guid userId)
    {
        // Generates a new KeyId
        _logger.LogInformation($"Key ID generated: {keyId}");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_securityKey);

        if (key.Length < 16)
        {
            throw new InvalidOperationException("The key is too short.");
        }


        var securityKey = new SymmetricSecurityKey(key) { KeyId = "4b623c772ff94971e1b1bb0723b2a0cb" };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }






    public Guid GetUserIdFromToken(string token)
    {
        // DEBUG ONLY: Show PII (Personally Identifiable Information)
        IdentityModelEventSource.ShowPII = true;
        var tokenValidationParameters = _tokenValidationService.GetTokenValidationParameters();
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
        catch (SecurityTokenValidationException stEx)
        {


            // Log the detailed exception and rethrow or handle accordingly
            Console.WriteLine($"Security token validation error: {stEx.Message}");
            throw;
        }
        catch (ArgumentException argEx)
        {
            // This will catch issues like an invalid GUID
            Console.WriteLine($"Argument exception, check if the GUID is correct: {argEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General exception occurred: {ex}");
            throw;
        }

    }


    public async Task<string> GenerateRefreshToken(string email, bool rememberme)
    {
        var user = await _usersRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(randomNumber),
            UserId = user.Id,
            Expires = rememberme ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddDays(7), // For example, 30 days if rememberMe is true, otherwise 7 days
            Created = DateTime.UtcNow
        };

        //  await _usersRepository.SaveRefreshToken(refreshToken);

        return refreshToken.Token;
    }


}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}



