using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Auth.Services;
using CounterPick.Core.Data;
using CounterPick.Core.DTOs;
using CounterPick.Core.Models;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        TokenService tokenService,
        AppDbContext db,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _db = db;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Username);
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt: unknown user '{Username}'", dto.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }
        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            _logger.LogWarning("Failed login attempt for '{Username}': wrong password", dto.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return await GenerateTokenResponse(user);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new IdentityUser { UserName = dto.Username, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                _logger.LogWarning("Registration failed for '{Username}': {Code} - {Desc}", dto.Username, err.Code, err.Description);
            return BadRequest(result.Errors);
        }

        _logger.LogInformation("User '{Username}' registered successfully", dto.Username);
        return await GenerateTokenResponse(user);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        //#baka if someone presents a revoked token, it likely means a thief stole it — revoke ALL tokens for that user to kick them out
        if (stored is null)
        {
            _logger.LogWarning("Refresh attempt with non-existent token");
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }
        if (stored.IsRevoked)
        {
            _logger.LogWarning("Token theft detected for user {UserId} — revoking all tokens", stored.UserId);
            var allUserTokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == stored.UserId && !rt.IsRevoked)
                .ToListAsync();
            foreach (var t in allUserTokens) t.IsRevoked = true;
            await _db.SaveChangesAsync();
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }
        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh attempt with expired token for user {UserId}", stored.UserId);
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        stored.IsRevoked = true;
        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null)
        {
            _logger.LogWarning("Refresh token user {UserId} not found", stored.UserId);
            return Unauthorized();
        }

        return await GenerateTokenResponse(user);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequestDto dto)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

        if (stored is not null)
        {
        //#baka mark old token as revoked (one-time-use), then issue a brand new pair
        stored.IsRevoked = true;
            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpGet("external-login/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var redirectUrl = Url.Action("ExternalCallback", "Auth");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    //#baka OAuth callback: user gets redirected here by Google/MS after consent; we extract their identity from the external provider's token
    [HttpGet("external-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalCallback()
    {
        //#baka GetExternalLoginInfoAsync reads the auth cookies set during the Challenge() redirect; returns null if flow failed
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            _logger.LogWarning("External auth callback failed: no login info");
            return Redirect("/login.html#error=external-auth-failed");
        }

        //#baka try to sign in with the external login provider directly (user already linked their Google/MS account before)
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false);

        IdentityUser user;
        if (result.Succeeded)
        {
            user = (await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey))!;
        }
        else
        {
            //#baka first-time external login — we need to find or create a local user by email
            var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                     ?? info.Principal.FindFirstValue("email")
                     ?? info.Principal.FindFirstValue("preferred_username");

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("External auth failed: email not provided");
                return Redirect("/login.html#error=email-not-provided");
            }

            user = (await _userManager.FindByEmailAsync(email))!;
            if (user is null)
            {
                user = new IdentityUser { UserName = email, Email = email };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("External auth: failed to create user for {Email}", email);
                    return Redirect("/login.html#error=registration-failed");
                }
            }

            //#baka link the external login (Google/MS) to the local user so next time ExternalLoginSignInAsync succeeds
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("External auth: failed to link login for {Email}", email);
                return Redirect("/login.html#error=link-failed");
            }

            await _userManager.AddClaimAsync(user, new Claim("ExternalLogin", "true"));
        }

        return Redirect(await GenerateTokenFragment(user));
    }

    private async Task<string> GenerateTokenFragment(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email ?? "")
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        return $"/login.html#access_token={accessToken}&refresh_token={refreshToken}";
    }

    private async Task<IActionResult> GenerateTokenResponse(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email ?? "")
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _db.SaveChangesAsync();

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        });
    }
}
