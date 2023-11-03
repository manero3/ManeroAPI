using System.IdentityModel.Tokens.Jwt;
using ManeroBackendAPI.Models.Entities;
using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Enums;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Models.DTOs;
using Newtonsoft.Json;


namespace ManeroBackendAPI.Services;

public interface IExternalAuthService
{
    Task<ServiceResponse<ApplicationUser>> AuthenticateWithGoogleAsync(string code);
}

public class ExternalAuthService : IExternalAuthService
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalAuthService(IUserService userService, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _userService = userService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ServiceResponse<ApplicationUser>> AuthenticateWithGoogleAsync(string code)
    {
        var tokenResponse = await GetGoogleTokensAsync(code);
        var googleUser = DecodeGoogleIdToken(tokenResponse.IdToken);

        var response = await _userService.GetUserByEmailAsync(googleUser.Email);
        var existingUser = response.Content;

        if (existingUser != null)
        {
            return new ServiceResponse<ApplicationUser>
            {
                StatusCode = StatusCode.Ok,
                Content = existingUser
            };
        }

        var newUserDto = new UserRegistrationDto
        {
            Email = googleUser.Email,
            OAuthId = googleUser.Sub,
            OAuthProvider = "Google",
            // ... (map other required fields)
        };

        await _userService.CreateAsync(new ServiceRequest<UserRegistrationDto> { Content = newUserDto });

        var newUserEntity = new ApplicationUser
        {
            Email = newUserDto.Email,
            OAuthId = newUserDto.OAuthId,
            OAuthProvider = newUserDto.OAuthProvider,

        };

        return new ServiceResponse<ApplicationUser>
        {
            Content = newUserEntity,
            StatusCode = StatusCode.Created
        };
    }

    private async Task<TokenResponse> GetGoogleTokensAsync(string code)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];

        var tokenRequestValues = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("client_id", googleClientId!),
        new KeyValuePair<string, string>("client_secret", googleClientSecret!),
        new KeyValuePair<string, string>("redirect_uri", "https://localhost:7286/signin-google"),
        new KeyValuePair<string, string>("grant_type", "authorization_code")
    };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(tokenRequestValues)
        };

        var response = await httpClient.SendAsync(requestMessage);
        var responseString = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<TokenResponse>(responseString)!;
    }


    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("id_token")]
        public string IdToken { get; set; }


    }
    private GoogleUser DecodeGoogleIdToken(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(idToken);

        return new GoogleUser
        {

            Email = jwtToken.Claims.First(claim => claim.Type == "email").Value,
            Sub = jwtToken.Claims.First(claim => claim.Type == "sub").Value
        };
    }

    public class GoogleUser
    {
        public string Email { get; set; }
        public string Sub { get; set; } // This is Google's unique ID for the user.
    }


}
