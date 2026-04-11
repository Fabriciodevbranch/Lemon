using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Users.Commands;
using LemonWriter.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.Name, request.Password);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Conflict(new { error = result.Error!.Message });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var credentialsValid = await _authService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (!credentialsValid)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = await _mediator.Send(new GetUserByEmailQuery(request.Email), cancellationToken);
        if (result.IsFailure)
            return Unauthorized(new { error = "Invalid credentials." });

        var token = GenerateJwtToken(result.Value!.Id, result.Value.Email, result.Value.Name);
        return Ok(new { token, user = result.Value });
    }

    [HttpGet("oauth/google/callback")]
    public IActionResult GoogleCallback()
    {
        // OAuth callback is handled by the OAuth middleware.
        // This endpoint is a placeholder for documentation.
        return Ok(new { message = "OAuth callback endpoint." });
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
}

public record RegisterRequest(string Email, string Name, string Password);
public record LoginRequest(string Email, string Password);
