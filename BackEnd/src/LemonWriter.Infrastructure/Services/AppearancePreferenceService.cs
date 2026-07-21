using System.Text.Json;
using System.Text.RegularExpressions;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class AppearancePreferenceService(LemonDbContext db) : IAppearancePreferenceService
{
    private static readonly HashSet<string> Themes = ["cozy-amber", "cozy-forest", "cozy-night", "custom"];
    private static readonly HashSet<string> Variables =
    [
        "--custom-bg-color", "--custom-surface-color", "--custom-primary-color", "--custom-primary-light",
        "--custom-text-primary", "--custom-text-secondary", "--custom-border-color"
    ];
    private static readonly Regex Color = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public async Task<AppearancePreferenceDto?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return null;
        var variables = string.IsNullOrWhiteSpace(user.CustomThemeVariables)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(user.CustomThemeVariables) ?? [];
        return new(user.ThemePreference, variables);
    }

    public async Task<Result> SetAsync(Guid userId, AppearancePreferenceDto preference, CancellationToken ct = default)
    {
        if (preference.Theme is null || !Themes.Contains(preference.Theme))
            return Result.Failure(Error.Custom("INVALID_THEME", "Choose a valid theme."));
        if (preference.CustomVariables.Any(x => !Variables.Contains(x.Key) || !Color.IsMatch(x.Value)))
            return Result.Failure(Error.Custom("INVALID_THEME_COLOR", "One or more custom theme colors are invalid."));
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return Result.Failure(Error.NotFound);
        user.SetAppearance(preference.Theme, JsonSerializer.Serialize(preference.CustomVariables));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
