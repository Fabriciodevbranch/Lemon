using LemonWriter.Domain.Common;

namespace LemonWriter.Domain.Entities;

public class User : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? OAuthProvider { get; private set; }
    public string? OAuthProviderId { get; private set; }
    public string? PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IncludeExportBranding { get; private set; } = true;
    public bool StoryMetricsEnabled { get; private set; }
    public string? ThemePreference { get; private set; }
    public string? CustomThemeVariables { get; private set; }

    private User() { }

    public static User Create(
        string email,
        string name,
        string? oAuthProvider = null,
        string? oAuthProviderId = null,
        string? passwordHash = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            OAuthProvider = oAuthProvider,
            OAuthProviderId = oAuthProviderId,
            PasswordHash = passwordHash,
            IncludeExportBranding = true,
            StoryMetricsEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string name) => Name = name;
    public void SetExportBranding(bool include) => IncludeExportBranding = include;
    public void SetStoryMetrics(bool enabled) => StoryMetricsEnabled = enabled;
    public void SetAppearance(string theme, string customThemeVariables)
    { ThemePreference = theme; CustomThemeVariables = customThemeVariables; }
}
