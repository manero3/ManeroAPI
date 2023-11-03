using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ManeroBackendAPI.Models.Entities;
using ManeroBackendAPI.Authentication;
using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Enums;
using ManeroBackendAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ManeroBackendAPI.Services;

public class GenerateTokenService
{
    private readonly IOptions<JwtSettings> _jwtSettings;

    public GenerateTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public string GenerateJWTForUser(string userId)
    {
        var secret = _jwtSettings.Value.Key;
        // If your secret is base64 encoded in the configuration, decode it
        var secretKey = new SymmetricSecurityKey(Convert.FromBase64String(secret));

        var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId)
    };

        var tokenOptions = new JwtSecurityToken(
                       issuer: "https://localhost:7286",
                       audience: "https://localhost:7286",
                       claims: claims,
                       expires: DateTime.Now.AddMinutes(5),
                       signingCredentials: signinCredentials
                   );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        return tokenString;
    }


    public Task<ServiceResponse<string>> CreateUserAndReturnToken(ApplicationUser newUser)
    {
        var token = GenerateJWTForUser(newUser.Id.ToString());
        return Task.FromResult(new ServiceResponse<string>
        {
            StatusCode = StatusCode.Ok,
            Content = token
        });
    }

}
