using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class LibraryQueryService(LemonDbContext db) : ILibraryQueryService
{
    public async Task<IReadOnlyDictionary<Guid, LibraryBookStatsDto>> GetBookStatsAsync(IEnumerable<Guid> bookIds, CancellationToken ct = default)
    {
        var ids = bookIds.Distinct().ToArray();
        var chapters = await db.Chapters.AsNoTracking().Where(x => ids.Contains(x.BookId))
            .Select(x => new { x.BookId, x.CurrentContent }).ToListAsync(ct);
        return chapters.GroupBy(x => x.BookId).ToDictionary(x => x.Key, x => new LibraryBookStatsDto(
            x.Count(), x.Sum(chapter => string.IsNullOrWhiteSpace(chapter.CurrentContent) ? 0 : chapter.CurrentContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length)));
    }
}

public sealed class PrivacyPreferenceService(LemonDbContext db) : IPrivacyPreferenceService
{
    public Task<bool?> GetStoryMetricsEnabledAsync(Guid userId, CancellationToken ct = default) =>
        db.Users.Where(x => x.Id == userId).Select(x => (bool?)x.StoryMetricsEnabled).SingleOrDefaultAsync(ct);

    public async Task<Result> SetStoryMetricsEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return Result.Failure(Error.NotFound);
        user.SetStoryMetrics(enabled);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
