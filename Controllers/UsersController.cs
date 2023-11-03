using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using ManeroBackendAPI.Models;
using ManeroBackendAPI.Models.Entities;
using ManeroBackendAPI.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ManeroBackendAPI.Models.DTOs;
using ManeroBackendAPI.Contexts;

using static ManeroBackendAPI.Services.ExternalAuthService;

namespace ManeroBackendAPI.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IGoogleTokenService _googleTokenService;
    private readonly ApplicationDBContext _context;
    private readonly ITokenService _tokenService;
   

    public UsersController(IUserService userService, IGoogleTokenService googleTokenService, ApplicationDBContext context, ITokenService tokenService)
    {
        _userService = userService;
        _googleTokenService = googleTokenService;
        _context = context;
        _tokenService = tokenService;
        
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegistrationDto registrationDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ModelState.Remove("OAuthId");
            ModelState.Remove("OAuthProvider");

            // Await the asynchronous method
            var existingUserResponse = await _userService.GetUserByEmailAsync(registrationDto.Email);
            if (existingUserResponse.Content != null)
            {
                // Changed from BadRequest to Conflict
                return Conflict("Email is already in use.");
            }

            var response = await _userService.CreateAsync(new ServiceRequest<UserRegistrationDto>
            {
                Content = registrationDto
            });

            return StatusCode((int)response.StatusCode, response);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return Problem();
        }
    }

    [HttpPost("signup-google")]
    public IActionResult SignUpGoogle([FromBody] TokenRequest request)
    {
        if (request != null && !string.IsNullOrEmpty(request.Code))
        {
            return Ok("Received the code!");
        }
        return BadRequest("Code not received.");
    }

    [HttpGet("allusers")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return Problem();
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto loginDto)
    {
       
        var response = await _userService.LoginAsync(loginDto);

        if (response.Content == null)
        {
            return Unauthorized();  // Return 401 Unauthorized if login fails
        }

        // Assuming response.Content contains the JWT token
        var token = response.Content.JwtToken;

        // Decode token to get user ID or use the user service to get user details
        var userId = _tokenService.GetUserIdFromToken(token!);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound("User not found."); // This should not happen, just in case
        }

        // Now return the token along with user details
        return Ok(new
        {
            Token = token,
            User = new
            {
                user.Id,
                user.Email,
                FullName = user.Email // Use the actual property for the user's full name
            }
        });
    }




}
