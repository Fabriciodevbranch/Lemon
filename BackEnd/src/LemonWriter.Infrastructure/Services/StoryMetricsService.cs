using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class StoryMetricsService(LemonDbContext db) : IStoryMetricsService
{
    public async Task<StoryMetricsDto> GetMetricsAsync(Guid bookId, Guid userId, CancellationToken ct = default)
    {
        var enabled = await db.Users.Where(x => x.Id == userId).Select(x => x.StoryMetricsEnabled).SingleOrDefaultAsync(ct);
        if (!enabled) return new(false);
        var entries = await db.StoryStudioEntries.AsNoTracking().Where(x => x.BookId == bookId).ToListAsync(ct);
        var counts = entries.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
        int Get(string key) => counts.GetValueOrDefault(key);
        var characters = entries.Where(x => x.Type == "characters").ToList();
        var places = entries.Where(x => x.Type == "places").ToList();
        var objects = entries.Where(x => x.Type == "objects").ToList();
        var timeline = entries.Where(x => x.Type == "timeline").ToList();
        var edges = await db.StoryRelationships.AsNoTracking().Where(x => x.BookId == bookId)
            .Select(x => new { x.FromEntryId, x.ToEntryId }).ToListAsync(ct);
        var linkedIds = edges.SelectMany(x => new[] { x.FromEntryId, x.ToEntryId }).Distinct().ToList();
        static int Coverage<T>(IReadOnlyCollection<T> all, Func<T, bool> complete) => all.Count == 0 ? 0 : (int)Math.Round(all.Count(complete) * 100d / all.Count);
        var characterCoverage = Coverage(characters, x => !string.IsNullOrWhiteSpace(x.Details) && (!string.IsNullOrWhiteSpace(x.Motivation) || !string.IsNullOrWhiteSpace(x.Plot)));
        var relationshipCoverage = characters.Count == 0 ? 0 : (int)Math.Round(linkedIds.Count * 100d / characters.Count);
        var timelineCompleteness = Coverage(timeline, x => !string.IsNullOrWhiteSpace(x.Summary) && !string.IsNullOrWhiteSpace(x.Details));
        var worldbuilding = Math.Min(100, Get("characters") * 10 + Get("places") * 8 + Get("objects") * 5 + Get("lore") * 4 + Get("magic") * 4);
        return new(true, Get("characters"), Get("places"), Get("objects"), Get("gallery"), edges.Count, worldbuilding,
            (worldbuilding + characterCoverage + relationshipCoverage + timelineCompleteness) / 4, characterCoverage,
            Math.Min(100, relationshipCoverage), timelineCompleteness, Math.Max(0, characters.Count - linkedIds.Count),
            places.Count(x => string.IsNullOrWhiteSpace(x.Details)), objects.Count(x => string.IsNullOrWhiteSpace(x.Plot)));
    }
}
