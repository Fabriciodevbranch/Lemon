using LemonWriter.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LemonWriter.API.Controllers;

[ApiController]
[Authorize]
[Route("api/privacy")]
public sealed class PrivacyController : ControllerBase
{
    private readonly LemonDbContext _db;
    public PrivacyController(LemonDbContext db) => _db = db;

    [HttpGet("story-metrics")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await CurrentUser(ct);
        return user is null ? Unauthorized() : Ok(new { enabled = user.StoryMetricsEnabled });
    }

    [HttpPut("story-metrics")]
    public async Task<IActionResult> Set(MetricsPreference request, CancellationToken ct)
    {
        var user = await CurrentUser(ct);
        if (user is null) return Unauthorized();
        user.SetStoryMetrics(request.Enabled);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private Task<LemonWriter.Domain.Entities.User?> CurrentUser(CancellationToken ct)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? _db.Users.SingleOrDefaultAsync(x => x.Id == id, ct) : Task.FromResult<LemonWriter.Domain.Entities.User?>(null);
    }
}

public record MetricsPreference(bool Enabled);
