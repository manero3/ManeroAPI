using System.Diagnostics;
using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using ManeroBackendAPI.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManeroBackendAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly ApplicationDBContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDBContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }


    [HttpGet("getuserinfo")]
    [Authorize]
    public async Task<IActionResult> GetUserInfo()
    {
        var headers = Request.Headers;
        foreach (var header in headers)
        {
            _logger.LogInformation($"{header.Key}: {header.Value}");
        }
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Debug.WriteLine($"User ID: {userIdString}");
        //  if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return BadRequest("Invalid user ID format");
        }

        var user = await _context.Users.FindAsync(userId); // Use FindAsync for async operation
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Email,
            // Include other user details you want to return
        });
    }


    [HttpGet("user")]
    public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
    {
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email is required.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            return NotFound("User not found.");

        // Return user data. Add more fields as needed.
        return Ok(new
        {
            user.Id,
            user.Email,
            fullName = user.Email
        });
    }
}
