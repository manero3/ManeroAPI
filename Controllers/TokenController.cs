using ManeroBackendAPI.Services;
using ManeroBackendAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ManeroBackendAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TokenController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("gettoken")]
    public async Task<IActionResult> GetToken(UserLoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _tokenService.GetTokenAsync(model.Email, model.Password, (bool)model.RememberMe);
        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return BadRequest(ModelState);
        }

        return Ok(new { token });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var newToken = await _tokenService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
            return Ok(new { token = newToken });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
