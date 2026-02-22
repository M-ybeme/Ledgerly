using Ledgerly.Api.Auth;
using Ledgerly.Application.Auth;
using Ledgerly.Contracts.Auth;
using Ledgerly.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

    public AuthController(
        UserManager<ApplicationUser> userManager,
        JwtTokenService jwt,
        IEmailService email,
        IConfiguration config)
    {
        _userManager = userManager;
        _jwt = jwt;
        _email = email;
        _config = config;
    }

    // POST /auth/register
    [HttpPost("register")]
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
    public async Task<ActionResult<AuthTokenDto>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        if (!await _userManager.CheckPasswordAsync(user, req.Password))
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        if (!await _userManager.IsEmailConfirmedAsync(user))
            return Problem(detail: "Email not confirmed. Please check your inbox.", statusCode: StatusCodes.Status403Forbidden);

        return Ok(_jwt.GenerateToken(user));
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

    // POST /auth/forgot-password
    [HttpPost("forgot-password")]
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
}
