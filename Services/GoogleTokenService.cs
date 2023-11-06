using System.Diagnostics;
using System.Security;
using Google.Apis.Auth;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Azure.Core;
using System.Net.Http.Headers;
using ManeroBackendAPI.Models.DTOs;
using ManeroBackendAPI.Enums;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Authentication;

namespace ManeroBackendAPI.Services;

public interface IGoogleTokenService
{

    Task<ServiceResponse<GoogleUser>> CreateGoogleUserAsync(GoogleUser user);
    Task<ServiceResponse<string>> ExchangeCodeForTokenAsync(TokenRequest request);
    Task<ServiceResponse<TokenRequest>> GetTokenFromCodeAsync(TokenRequest request);
    Task<ServiceResponse<GoogleUser>> GetGoogleUserFromTokenAsync(string token);




}
public class GoogleTokenService : IGoogleTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly HttpClient _httpClient;

    public GoogleTokenService(IConfiguration configuration, IUserService userService, HttpClient httpClient)
    {
        _configuration = configuration;
        _userService = userService;
        _httpClient = httpClient;
    }


    public async Task<ServiceResponse<string>> ExchangeCodeForTokenAsync([FromBody] TokenRequest request)
    {
        var code = request.Code;
        var tokenEndpoint = "https://oauth2.googleapis.com/token";
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];

        var postData = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("client_id", googleClientId),
        new KeyValuePair<string, string>("client_secret", googleClientSecret),
        new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/auth/callback"),
        new KeyValuePair<string, string>("grant_type", "authorization_code")
    };

        // Set headers for the HTTP client
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(postData));

        // Read response content regardless of success status
        var responseBody = await response.Content.ReadAsStringAsync();

        // Check if the response was not successful
        if (!response.IsSuccessStatusCode)
        {
            // Log the exact error from Google's response for debugging
            Console.WriteLine($"Error from Google API: {responseBody}");

            return new ServiceResponse<string>
            {
                StatusCode = Enums.StatusCode.BadRequest,
                Content = null,
                Message = $"Error from Google API: {responseBody}"
            };
        }



        // Placeholder for success scenario
        return new ServiceResponse<string>
        {
            StatusCode = Enums.StatusCode.Ok,
            Content = "Token processed successfully.",
            Message = responseBody
        };
    }

    private string GeneratePlaceholderPassword()
    {

        return "RegisteredThroughGoogle";
    }
    public async Task<ServiceResponse<GoogleUser>> CreateGoogleUserAsync(GoogleUser user)
    {


        // Map GoogleUser to your registration DTO
        var newUserDto = new OAuthRegistrationDTO
        {
            Email = user.Email,
            OAuthId = user.UserId,
            OAuthProvider = "Google",
            Password = GeneratePlaceholderPassword()
        };

        // Create the user using your user service
        var serviceResponse = await _userService.CreateGoogleUserAsync(new ServiceRequest<OAuthRegistrationDTO> { Content = newUserDto });

        // If user creation was successful, return the GoogleUser.
        if (serviceResponse.StatusCode == Enums.StatusCode.Created)
        {
            return new ServiceResponse<GoogleUser>
            {
                StatusCode = Enums.StatusCode.Created,
                Content = user
            };
        }
        else
        {

            return new ServiceResponse<GoogleUser>
            {
                StatusCode = Enums.StatusCode.InternalServerError,
                Content = null,
                Message = "Failed to create user."
            };
        }
    }

    public async Task<ServiceResponse<TokenRequest>> GetTokenFromCodeAsync(TokenRequest request)
    {

        var token = await ExchangeCodeForTokenAsync(request);
        var response = new ServiceResponse<TokenRequest>
        {
            StatusCode = (token.StatusCode == Enums.StatusCode.Ok) ? Enums.StatusCode.Ok : Enums.StatusCode.BadRequest,
            Content = request,
            Message = token.Message
        };

        return response;
    }

    public async Task<ServiceResponse<GoogleUser>> GetGoogleUserFromTokenAsync(string token)
    {
        var userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

        // Set the authorization header with the token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync(userInfoEndpoint);

        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResponse<GoogleUser>
            {
                StatusCode = Enums.StatusCode.BadRequest,
                Content = null,
                Message = "Failed to fetch Google user with token."
            };
        }

        var userInfo = await response.Content.ReadAsAsync<GoogleUserResponse>();

        var googleUser = new GoogleUser
        {
            UserId = userInfo.sub,
            Email = userInfo.email,
            EmailVerified = userInfo.email_verified,
            Name = userInfo.name,
            PictureUrl = userInfo.picture,
            Locale = userInfo.locale,
            FamilyName = userInfo.family_name,
            GivenName = userInfo.given_name
        };

        return new ServiceResponse<GoogleUser>
        {
            StatusCode = Enums.StatusCode.Ok,
            Content = googleUser
        };
    }


    private class GoogleUserResponse
    {
        public string sub { get; set; }
        public string email { get; set; }
        public bool email_verified { get; set; }
        public string name { get; set; }
        public string picture { get; set; }
        public string locale { get; set; }
        public string family_name { get; set; }
        public string given_name { get; set; }
    }


}
