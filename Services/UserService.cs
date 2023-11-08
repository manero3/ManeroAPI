using System.Diagnostics;
using ManeroBackendAPI.Models.Entities;
using ManeroBackendAPI.Authentication;
using ManeroBackendAPI.Authorization;
using ManeroBackendAPI.Contexts;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Models.DTOs;
using ManeroBackendAPI.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ManeroBackendAPI.Services;



public interface IUserService
{
    Task<ServiceResponse<UserWithTokenResponse>> CreateAsync(ServiceRequest<UserRegistrationDto> request);
    Task<ServiceResponse<UserWithTokenResponse>> CreateGoogleUserAsync(ServiceRequest<OAuthRegistrationDTO> request);
    Task<ServiceResponse<ApplicationUser>> GetUserByEmailAsync(string email);
    Task<ServiceResponse<IEnumerable<ApplicationUser>>> GetAllAsync();
    Task<ServiceResponse<UserLoginDto>> LoginAsync(UserLoginDto loginDto);




}

public class UserService : IUserService
{
    private readonly IUsersRepository _usersRepository;
    private readonly GenerateTokenService _generateTokenService;
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(IUsersRepository usersRepository, GenerateTokenService generateTokenService, ITokenService tokenService, UserManager<ApplicationUser> userManager)
    {
        _usersRepository = usersRepository;
        _generateTokenService = generateTokenService;
        _tokenService = tokenService;
        _userManager = userManager;
    }

    public async Task<ApplicationUser> CreateAccount(ApplicationUser user)
    {
        return await _usersRepository.CreateAsync(user);
    }

    public async Task<ServiceResponse<UserWithTokenResponse>> CreateAsync(ServiceRequest<UserRegistrationDto> request)
    {
        var response = new ServiceResponse<UserWithTokenResponse>();

        try
        {
            if (request.Content == null)
            {
                response.StatusCode = Enums.StatusCode.BadRequest;
                response.Message = "Content cannot be null.";
                return response;
            }

            // Use UserManager to check if user exists and create the user
            var userExists = await _userManager.FindByEmailAsync(request.Content.Email);
            if (userExists != null)
            {
                response.StatusCode = Enums.StatusCode.Conflict;
                response.Message = "User already exists.";
                return response;
            }

            ApplicationUser newUser = new ApplicationUser
            {
                UserName = request.Content.Email,
                Email = request.Content.Email,
                FirstName = request.Content.FirstName,
                LastName = request.Content.LastName,
                EmailConfirmed = true // Usually, you would want to send a confirmation email to set this to true
            };

            var result = await _userManager.CreateAsync(newUser, request.Content.Password);
            if (!result.Succeeded)
            {
                response.StatusCode = Enums.StatusCode.Conflict;
                response.Message = "User could not be created.";
                return response;
            }

            // Generate token after successful creation
            var tokenResponse = await _generateTokenService.CreateUserAndReturnToken(newUser);
            if (tokenResponse.StatusCode != Enums.StatusCode.Ok)
            {
                // Ideally, you might want to rollback user creation if token generation fails
                return new ServiceResponse<UserWithTokenResponse>
                {
                    StatusCode = tokenResponse.StatusCode,
                    Message = tokenResponse.Message
                };
            }

            response.Content = new UserWithTokenResponse
            {
                User = newUser,
                Token = tokenResponse.Content!
            };

            response.StatusCode = Enums.StatusCode.Created;
        }
        catch (Exception ex)
        {
            response.StatusCode = Enums.StatusCode.InternalServerError;
            response.Message = ex.Message;
        }

        return response;
    }



    public async Task<ServiceResponse<IEnumerable<ApplicationUser>>> GetAllAsync()
    {
        var response = new ServiceResponse<IEnumerable<ApplicationUser>>();

        try
        {
            response.Content = await _usersRepository.GetAllAsync();
            response.StatusCode = Enums.StatusCode.Ok;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = Enums.StatusCode.InternalServerError;
            response.Message = ex.Message;
        }

        return response;
    }


    public async Task<ServiceResponse<ApplicationUser>> GetUserByEmailAsync(string email)
    {
        var response = new ServiceResponse<ApplicationUser>();

        try
        {
            if (string.IsNullOrEmpty(email))
            {
                response.StatusCode = Enums.StatusCode.BadRequest;
                response.Message = "Email cannot be null or empty.";
                return response;
            }


            response.Content = await _usersRepository.GetUserByEmailAsync(email);

            if (response.Content == null)
            {
                response.StatusCode = Enums.StatusCode.NotFound;
                response.Message = "User not found.";
            }
            else
            {
                response.StatusCode = Enums.StatusCode.Ok;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            response.StatusCode = Enums.StatusCode.InternalServerError;
            response.Message = ex.Message;
        }

        return response;
    }

    public async Task<ServiceResponse<UserWithTokenResponse>> CreateGoogleUserAsync(ServiceRequest<OAuthRegistrationDTO> request)
    {
        var response = new ServiceResponse<UserWithTokenResponse>();

        try
        {
            if (request.Content == null)
            {
                response.StatusCode = Enums.StatusCode.BadRequest;
                response.Message = "Content cannot be null.";
                return response;
            }

            if (!await _usersRepository.ExistsAsync(x => x.Email == request.Content.Email))
            {
                // Convert OAuthRegistrationDTO to Entity
                ApplicationUser newUser = new ApplicationUser
                {
                    Email = request.Content.Email,
                    OAuthId = request.Content.OAuthId,
                    OAuthProvider = request.Content.OAuthProvider,
                    PasswordHash = "GoogleUserPWD",
                };

                var createdUser = await _usersRepository.CreateAsync(newUser);
                if (createdUser == null) // Assuming your CreateAsync method returns null in case of failure.
                {
                    response.StatusCode = Enums.StatusCode.Conflict;
                    response.Message = "User could not be created.";
                    return response;
                }

                var tokenResponse = await _generateTokenService.CreateUserAndReturnToken(newUser);
                if (tokenResponse.StatusCode != Enums.StatusCode.Ok)
                {
                    return new ServiceResponse<UserWithTokenResponse>
                    {
                        StatusCode = tokenResponse.StatusCode,
                        Message = tokenResponse.Message
                    };
                }

                response.Content = new UserWithTokenResponse
                {
                    User = createdUser,
                    Token = tokenResponse.Content!
                };

                response.StatusCode = Enums.StatusCode.Created;
            }
            else
            {
                response.StatusCode = Enums.StatusCode.Conflict;
                response.Message = "User already exists.";
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = Enums.StatusCode.InternalServerError;
            response.Message = ex.Message;
        }

        return response;
    }


    public async Task<ServiceResponse<UserLoginDto>> LoginAsync(UserLoginDto loginDto)
    {
        var userResponse = await GetUserByEmailAsync(loginDto.Email);
        var user = userResponse.Content;

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return new ServiceResponse<UserLoginDto>
            {
                StatusCode = Enums.StatusCode.Unauthorized,
                Message = "Invalid email or password"
            };
        }

        // Since TokenService is already generating a refresh token, retrieve it
        // Since TokenService is now returning a UserWithTokenResponse object, retrieve it
        var tokenResponse = await _tokenService.GetTokenAsync(user.Email, loginDto.Password, loginDto.RememberMe ?? false);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
        {
            return new ServiceResponse<UserLoginDto>
            {
                StatusCode = Enums.StatusCode.Unauthorized,
                Message = "Failed to generate a token"
            };
        }

        // Now that we have a token response object, we can extract the tokens from it
        loginDto.JwtToken = tokenResponse.Token;
        loginDto.RefreshToken = tokenResponse.RefreshToken;

        return new ServiceResponse<UserLoginDto>
        {
            Content = loginDto,
            StatusCode = Enums.StatusCode.Ok
        };

    }




    private bool VerifyPassword(string password, string storedHash)
    {
        return PasswordHelper.VerifyPassword(password, storedHash);
    }



}
