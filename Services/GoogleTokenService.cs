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
    // Task<ServiceResponse<GoogleUser>> VerifyGoogleToken(string token);
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

    //public async Task<ServiceResponse<GoogleUser>> VerifyGoogleToken([FromBody] string token)
    //{
    //   try
    //    {
    //        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
    //        {
    //            Audience = new List<string> { _configuration["Authentication:Google:ClientId"]! }

    //        };

    //        GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);

    //        if (payload == null)
    //        {
    //            Console.WriteLine("Invalid Google Token.");
    //            return null!;
    //        }

    //        return new ServiceResponse<GoogleUser>
    //        {
    //            StatusCode = Enums.StatusCode.Ok,
    //            Content = new GoogleUser
    //            {
    //                UserId = payload.Subject,
    //                Email = payload.Email,
    //                EmailVerified = payload.EmailVerified,
    //                Name = payload.Name,
    //                PictureUrl = payload.Picture,
    //                Locale = payload.Locale,
    //                FamilyName = payload.FamilyName,
    //                GivenName = payload.GivenName
    //            }
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Token verification failed with error: {ex.Message}");
    //        return new ServiceResponse<GoogleUser>
    //        {
    //            StatusCode = Enums.StatusCode.BadRequest,
    //            Content = null,
    //            Message = $"Failed to verify token. Reason: {ex.Message}"
    //        };
    //    }

    //}
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
                StatusCode = StatusCode.BadRequest,
                Content = null,
                Message = $"Error from Google API: {responseBody}" // This will include Google's error message
            };
        }

        // Assuming success, the rest of your code to handle successful token retrieval would go here...

        // Placeholder for success scenario
        return new ServiceResponse<string>
        {
            StatusCode = StatusCode.Ok,
            Content = "Token processed successfully.",  // Adjust as needed.
            Message = responseBody // For now, just return the whole responseBody. Adjust as needed.
        };
    }

    private string GeneratePlaceholderPassword()
    {
        // This is just an example. You can create a random string or use any other placeholder value.
        return "RegisteredThroughGoogle";
    }
    public async Task<ServiceResponse<GoogleUser>> CreateGoogleUserAsync(GoogleUser user)
    {
        //// Check if user exists
        //var existingUser = await _userService.GetUserByEmailAsync(user.Email);
        //if (existingUser != null)
        //{
        //    // User already exists. Depending on your logic, you might want to 
        //    // simply return an indication that the user already exists.
        //    return new ServiceResponse<GoogleUser>
        //    {
        //        StatusCode = Enums.StatusCode.Conflict,
        //        Content = null,
        //        Message = "User already exists."
        //    };
        //}

        // Map GoogleUser to your registration DTO
        var newUserDto = new OAuthRegistrationDTO
        {
            Email = user.Email,
            OAuthId = user.UserId,  // Assuming `UserId` holds the unique Google ID
            OAuthProvider = "Google",
            Password = GeneratePlaceholderPassword()
        };

        // Create the user using your user service
        var serviceResponse = await _userService.CreateGoogleUserAsync(new ServiceRequest<OAuthRegistrationDTO> { Content = newUserDto });

        // If user creation was successful, return the GoogleUser.
        if (serviceResponse.StatusCode == StatusCode.Created)
        {
            return new ServiceResponse<GoogleUser>
            {
                StatusCode = StatusCode.Created,
                Content = user
            };
        }
        else
        {
            // Handle other cases where user creation was not successful. Adjust the StatusCode and Message as needed.
            return new ServiceResponse<GoogleUser>
            {
                StatusCode = StatusCode.InternalServerError, // Adjust this based on your enum values and the type of error.
                Content = null,
                Message = "Failed to create user."
            };
        }
    }

    public async Task<ServiceResponse<TokenRequest>> GetTokenFromCodeAsync(TokenRequest request)
    {
        // This can simply be an alias for the ExchangeCodeForTokenAsync method
        var token = await ExchangeCodeForTokenAsync(request);
        var response = new ServiceResponse<TokenRequest>
        {
            StatusCode = token.StatusCode == StatusCode.Ok ? StatusCode.Ok : StatusCode.BadRequest,
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
                StatusCode = StatusCode.BadRequest,
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
            StatusCode = StatusCode.Ok,
            Content = googleUser
        };
    }

    // This DTO matches the Google user info response. 
    // Adjust the property names and types as needed based on the actual response structure.
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
