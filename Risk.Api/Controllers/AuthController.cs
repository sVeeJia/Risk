using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risk.Api.Dtos.Requests;
using Risk.Api.Dtos.Responses;
using Risk.Api.Models;
using Risk.Api.Services.Interfaces;

namespace Risk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string CookieName = "risk_auth";

    private readonly IAuthService _auth;
    private readonly ITokenService _tokens;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService auth, ITokenService tokens, IWebHostEnvironment env)
    {
        _auth = auth;
        _tokens = tokens;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> RegisterAsync([FromBody] Register request)
    {
        var result = await _auth.RegisterAsync(request);
        if (result.Status == AuthStatus.EmailTaken)
        {
            return Conflict(new { message = "An account with that email already exists." });

        }

        SetAuthCookie(result.User!);
        return StatusCode(StatusCodes.Status201Created, ToResponse(result.User!));
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> LoginAsync([FromBody] Login request)
    {
        var result = await _auth.LoginAsync(request);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        SetAuthCookie(result.User!);
        return Ok(ToResponse(result.User!));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, BuildCookieOptions(expires: null));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserResponse> Me()
    {
        return Ok(new UserResponse
        {
            Id = User.FindFirst("sub")?.Value ?? string.Empty,
            Email = User.FindFirst("email")?.Value ?? string.Empty,
            DisplayName = User.FindFirst("name")?.Value ?? string.Empty
        });
    }

    private void SetAuthCookie(User user)
    {
        var token = _tokens.CreateToken(user);
        Response.Cookies.Append(CookieName, token.Token, BuildCookieOptions(token.ExpiresAt));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true, // JavaScript can never read it
        Secure = !_env.IsDevelopment(), // HTTPS only outside local development
        SameSite = SameSiteMode.Strict, // not sent on cross-site requests
        Path = "/",
        Expires = expires
    };

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName
    };
}