using ManeroBackendAPI.Controllers;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ManeroBackendAPI.Services;


public interface ITokenValidationService
{
    TokenValidationParameters GetTokenValidationParameters();
}

public class TokenValidationService : ITokenValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;

    public TokenValidationService(IConfiguration configuration, ILogger<AccountController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public TokenValidationParameters GetTokenValidationParameters()
    {

        SecurityKey ResolveKey(string tokenKid)
        {

            if (tokenKid == "4b623c772ff94971e1b1bb0723b2a0cb")
            {
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes("4b623c772ff94971e1b1bb0723b2a0cb"));
            }
            // If the 'kid' is not recognized, return null or throw an exception
            throw new SecurityTokenException("Invalid token 'kid' value.");
        }

        return new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                try
                {
                    _logger.LogInformation($"Attempting to resolve key for kid: {kid}");
                    var resolvedKey = ResolveKey(kid);
                    _logger.LogInformation($"Resolved key for kid: {kid}");
                    return new List<SecurityKey> { resolvedKey };
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error resolving key for kid: {kid}, Exception: {ex}");
                    throw;
                }
            },
            RequireSignedTokens = true,
            ValidateActor = false,
            TokenDecryptionKey = null,

        };

    }
}