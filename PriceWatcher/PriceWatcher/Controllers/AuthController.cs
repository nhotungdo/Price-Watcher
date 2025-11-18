using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Dtos;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(GoogleCallback)) : returnUrl;
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl ?? "/signin-google"
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("/signin-google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result?.Principal == null)
        {
            return Unauthorized();
        }

        var googleInfo = ExtractGoogleInfo(result.Principal);
        if (string.IsNullOrEmpty(googleInfo.Email))
        {
            return BadRequest("Google account email missing.");
        }

        var user = await _userService.GetOrCreateUserFromGoogleAsync(googleInfo, cancellationToken);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            result.Principal,
            result.Properties ?? new AuthenticationProperties());

        await _userService.OnLoginSuccessAsync(user, Request, cancellationToken);

        return Ok(new { userId = user.UserId, email = user.Email, fullName = user.FullName });
    }

    private static GoogleUserInfo ExtractGoogleInfo(ClaimsPrincipal principal)
    {
        return new GoogleUserInfo
        {
            GoogleId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Name = principal.FindFirstValue(ClaimTypes.Name),
            AvatarUrl = principal.FindFirstValue("picture")
        };
    }
}

