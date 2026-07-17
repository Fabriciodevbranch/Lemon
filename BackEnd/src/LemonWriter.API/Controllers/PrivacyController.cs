using LemonWriter.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LemonWriter.API.Controllers;

[ApiController]
[Authorize]
[Route("api/privacy")]
public sealed class PrivacyController : ControllerBase
{
    private readonly IPrivacyPreferenceService _preferences;
    private readonly ICurrentUserService _currentUser;
    public PrivacyController(IPrivacyPreferenceService preferences, ICurrentUserService currentUser)
    { _preferences = preferences; _currentUser = currentUser; }

    [HttpGet("story-metrics")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId) return Unauthorized();
        var enabled = await _preferences.GetStoryMetricsEnabledAsync(userId, ct);
        return enabled is null ? Unauthorized() : Ok(new { enabled });
    }

    [HttpPut("story-metrics")]
    public async Task<IActionResult> Set(MetricsPreference request, CancellationToken ct)
    {
        if (_currentUser.UserId is not Guid userId) return Unauthorized();
        var result = await _preferences.SetStoryMetricsEnabledAsync(userId, request.Enabled, ct);
        return result.IsSuccess ? NoContent() : Unauthorized();
    }
}

public record MetricsPreference(bool Enabled);
