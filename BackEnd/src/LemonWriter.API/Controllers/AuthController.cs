using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Users.Commands;
using LemonWriter.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Authentication endpoints for local and Google OAuth flows.
/// </summary>
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;

    public AuthController(IMediator mediator, IConfiguration configuration, IAuthService authService)
    {
        _mediator = mediator;
        _configuration = configuration;
        _authService = authService;
    }

    [HttpPost("register")]
    /// <summary>
    /// Registers a user and returns an access token.
    /// </summary>
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.DisplayName, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return Conflict(new { message = result.Error!.Message });

        var user = result.Value!;
        var token = GenerateJwtToken(user.Id, user.Email, user.Name);
        return Ok(new { token, user = ToClientUser(user) });
    }

    [HttpPost("login")]
    /// <summary>
    /// Authenticates a user and returns an access token.
    /// </summary>
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var credentialsValid = await _authService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (!credentialsValid)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = await _mediator.Send(new GetUserByEmailQuery(request.Email), cancellationToken);
        if (result.IsFailure)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = GenerateJwtToken(result.Value!.Id, result.Value.Email, result.Value.Name);
        return Ok(new { token, user = ToClientUser(result.Value) });
    }

    [HttpGet("oauth/google")]
    /// <summary>
    /// Starts Google OAuth login.
    /// </summary>
    public IActionResult GoogleLogin()
    {
        var callback = Url.Action(nameof(GoogleCallback), "Auth", null, Request.Scheme)!;
        return Challenge(new AuthenticationProperties { RedirectUri = callback }, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("oauth/google/complete")]
    /// <summary>
    /// Completes Google OAuth login and redirects to frontend callback URL.
    /// </summary>
    public async Task<IActionResult> GoogleCallback(CancellationToken cancellationToken)
    {
        var external = await HttpContext.AuthenticateAsync("External");
        if (!external.Succeeded || external.Principal is null)
            return Redirect("/auth/login?error=google");

        var email = external.Principal.FindFirstValue(ClaimTypes.Email);
        var name = external.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var providerId = external.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(email))
            return Redirect("/auth/login?error=google-email");

        var found = await _mediator.Send(new GetUserByEmailQuery(email), cancellationToken);
        var user = found.Value;
        if (found.IsFailure)
        {
            var registered = await _mediator.Send(
                new RegisterUserCommand(email, name ?? email, null, "Google", providerId), cancellationToken);
            if (registered.IsFailure)
                return Redirect("/auth/login?error=google-register");
            user = registered.Value;
        }

        await HttpContext.SignOutAsync("External");
        var token = GenerateJwtToken(user!.Id, user.Email, user.Name);
        var frontend = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var query =
            $"token={Uri.EscapeDataString(token)}&id={user.Id}&email={Uri.EscapeDataString(user.Email)}&displayName={Uri.EscapeDataString(user.Name)}";
        return Redirect($"{frontend}/auth/google-callback?{query}");
    }

    private string GenerateJwtToken(Guid userId, string email, string name)
    {
        var key = _configuration["Jwt:Key"] ?? "default-secret-key-replace-in-production";
        var issuer = _configuration["Jwt:Issuer"] ?? "LemonWriter";
        var audience = _configuration["Jwt:Audience"] ?? "LemonWriterClient";
        var expiry = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, name)
        };

        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var jwtToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(expiry), signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwtToken);
    }

    private static object ToClientUser(LemonWriter.Application.Common.DTOs.UserDto user) => new
    {
        user.Id,
        user.Email,
        DisplayName = user.Name,
        AvatarUrl = (string?)null,
        user.CreatedAt
    };
}

public record RegisterRequest(string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);
