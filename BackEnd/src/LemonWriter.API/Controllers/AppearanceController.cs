using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

/// <summary>
/// Endpoints for appearance preference management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/appearance")]
public sealed class AppearanceController(IAppearancePreferenceService preferences, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    /// <summary>
    /// Gets appearance preferences for the current user.
    /// </summary>
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) return Unauthorized();
        var preference = await preferences.GetAsync(userId, ct);
        return preference is null ? Unauthorized() : Ok(preference);
    }

    [HttpPut]
    /// <summary>
    /// Updates appearance preferences for the current user.
    /// </summary>
    public async Task<IActionResult> Set(AppearancePreferenceDto request, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) return Unauthorized();
        var result = await preferences.SetAsync(userId, request, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.Error!.Message });
    }
}
