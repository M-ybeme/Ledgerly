using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ledgerly.Api.Auth;
using Ledgerly.Application.Auth;
using Ledgerly.Contracts.Auth;
using Ledgerly.Infrastructure.Auth;
using Ledgerly.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwt;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly LedgerlyDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwt,
        IEmailService email,
        IConfiguration config,
        LedgerlyDbContext db)
    {
        _userManager = userManager;
        _jwt = jwt;
        _email = email;
        _config = config;
        _db = db;
    }

    // POST /auth/register
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var user = new ApplicationUser { UserName = req.Email, Email = req.Email };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Problem(detail: string.Join(" ", errors), statusCode: StatusCodes.Status400BadRequest);
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var webBase = _config["Web:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5001";
        var link = $"{webBase}/confirm-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

        await _email.SendAsync(
            user.Email!,
            "Confirm your Ledgerly account",
            $"<p>Welcome to Ledgerly!</p><p>Please confirm your email address by clicking the link below:</p><p><a href=\"{link}\">Confirm Email</a></p>");

        return StatusCode(StatusCodes.Status201Created, new { message = "Registration successful. Please check your email to confirm your account." });
    }

    // POST /auth/login
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        if (!await _userManager.CheckPasswordAsync(user, req.Password))
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return Problem(detail: "Email not confirmed. Please check your inbox.", statusCode: StatusCodes.Status403Forbidden);

        if (await _userManager.GetTwoFactorEnabledAsync(user))
            return Ok(new LoginResultDto(RequiresTwoFactor: true, Token: null, Email: req.Email, ExpiresUtc: default));

        var tokenDto = _jwt.GenerateToken(user);
        var refreshRaw = await IssueRefreshTokenAsync(user.Id);
        return Ok(new LoginResultDto(RequiresTwoFactor: false, Token: tokenDto.Token, Email: tokenDto.Email, ExpiresUtc: tokenDto.ExpiresUtc, RefreshToken: refreshRaw));
    }

    // POST /auth/2fa/verify-login
    [HttpPost("2fa/verify-login")]
    public async Task<ActionResult<AuthTokenDto>> VerifyTwoFactorLogin([FromBody] TwoFactorLoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(detail: "Invalid request.", statusCode: StatusCodes.Status401Unauthorized);

        if (!await _userManager.CheckPasswordAsync(user, req.Password))
            return Problem(detail: "Invalid request.", statusCode: StatusCodes.Status401Unauthorized);

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return Problem(detail: "Invalid request.", statusCode: StatusCodes.Status401Unauthorized);

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, req.Code.Replace(" ", "").Replace("-", ""));

        if (!isValid)
            return Problem(detail: "Invalid authenticator code.", statusCode: StatusCodes.Status401Unauthorized);

        var tokenDto = _jwt.GenerateToken(user);
        var refreshRaw = await IssueRefreshTokenAsync(user.Id);
        return Ok(tokenDto with { RefreshToken = refreshRaw });
    }

    // GET /auth/2fa/setup
    [HttpGet("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupDto>> GetTwoFactorSetup()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var issuer = "Ledgerly";
        var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email!)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6&algorithm=SHA1";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrBytes = qrCode.GetGraphic(20);
        var qrBase64 = Convert.ToBase64String(qrBytes);

        return Ok(new TwoFactorSetupDto(authenticatorUri, key!, qrBase64));
    }

    // GET /auth/2fa/status
    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        return Ok(new { isEnabled = await _userManager.GetTwoFactorEnabledAsync(user) });
    }

    // POST /auth/2fa/enable
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor([FromBody] VerifyTwoFactorRequest req)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, req.Code.Replace(" ", "").Replace("-", ""));

        if (!isValid)
            return Problem(detail: "Invalid authenticator code. Please try again.", statusCode: StatusCodes.Status400BadRequest);

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return Ok(new { message = "Two-factor authentication enabled." });
    }

    // POST /auth/2fa/disable
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        return Ok(new { message = "Two-factor authentication disabled." });
    }

    // GET /auth/confirm-email?email=&token=
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Problem(detail: "User not found.", statusCode: StatusCodes.Status400BadRequest);

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Problem(detail: string.Join(" ", errors), statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new { message = "Email confirmed. You can now log in." });
    }

    // POST /auth/resend-confirmation
    [HttpPost("resend-confirmation")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is not null && !await _userManager.IsEmailConfirmedAsync(user))
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var webBase = _config["Web:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5001";
            var link = $"{webBase}/confirm-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
            await _email.SendAsync(
                user.Email!,
                "Confirm your Ledgerly account",
                $"<p>Welcome to Ledgerly!</p><p>Please confirm your email address by clicking the link below:</p><p><a href=\"{link}\">Confirm Email</a></p>");
        }
        return Ok(new { message = "If that email is pending confirmation, a new link has been sent." });
    }

    // POST /auth/forgot-password
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is not null && await _userManager.IsEmailConfirmedAsync(user))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var webBase = _config["Web:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5001";
            var link = $"{webBase}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            await _email.SendAsync(
                user.Email!,
                "Reset your Ledgerly password",
                $"<p>We received a request to reset your password.</p><p><a href=\"{link}\">Reset Password</a></p><p>If you did not request this, you can ignore this email.</p>");
        }

        // Always return 200 to avoid user enumeration
        return Ok(new { message = "If that email is registered, a reset link has been sent." });
    }

    // POST /auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(detail: "Invalid request.", statusCode: StatusCodes.Status400BadRequest);

        var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Problem(detail: string.Join(" ", errors), statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new { message = "Password has been reset. You can now log in." });
    }

    // POST /auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return Problem(detail: string.Join(" ", errors), statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new { message = "Password changed successfully." });
    }

    // GET /auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        return Ok(new { id = user.Id, email = user.Email });
    }

    // POST /auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokenDto>> Refresh([FromBody] RefreshTokenRequest req)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(req.RefreshToken)));
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (stored is null || stored.IsRevoked || stored.ExpiresUtc < DateTime.UtcNow)
            return Problem(detail: "Invalid or expired refresh token.", statusCode: StatusCodes.Status401Unauthorized);

        // Rotate: revoke old, issue new
        stored.IsRevoked = true;
        var newRaw = await IssueRefreshTokenAsync(stored.UserId);
        await _db.SaveChangesAsync();

        var tokenDto = _jwt.GenerateToken(stored.User);
        return Ok(tokenDto with { RefreshToken = newRaw });
    }

    // POST /auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest req)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(req.RefreshToken)));
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
        return Ok(new { message = "Logged out." });
    }

    // GET /auth/google/login — initiates Google OAuth flow
    [HttpGet("google/login")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        // After middleware handles /signin-google, redirect to our complete endpoint
        var completeUrl = Url.Action(nameof(GoogleComplete), "Auth", null, Request.Scheme)!;
        var properties = new AuthenticationProperties { RedirectUri = completeUrl };
        return Challenge(properties, "Google");
    }

    // GET /auth/google/complete — called by middleware after OAuth succeeds
    [HttpGet("google/complete")]
    public async Task<IActionResult> GoogleComplete()
    {
        var webBase = _config["Web:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5136";

        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!result.Succeeded)
            return Redirect($"{webBase}/login?error=google_failed");

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return Redirect($"{webBase}/login?error=no_email");

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return Redirect($"{webBase}/login?error=registration_failed");
        }
        else if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        var tokenDto = _jwt.GenerateToken(user);
        var refreshRaw = await IssueRefreshTokenAsync(user.Id);
        await HttpContext.SignOutAsync("ExternalCookie");

        var redirectUrl = $"{webBase}/oauth-callback" +
            $"?token={Uri.EscapeDataString(tokenDto.Token)}" +
            $"&email={Uri.EscapeDataString(tokenDto.Email)}" +
            $"&refresh={Uri.EscapeDataString(refreshRaw)}";
        return Redirect(redirectUrl);
    }

    private async Task<string> IssueRefreshTokenAsync(Guid userId)
    {
        var (raw, hash) = JwtTokenService.GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresUtc = DateTime.UtcNow.AddDays(30),
        });
        await _db.SaveChangesAsync();
        return raw;
    }
}
