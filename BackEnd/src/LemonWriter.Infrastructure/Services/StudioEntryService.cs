using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class StudioEntryService(LemonDbContext db) : IStudioEntryService
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
        { "characters", "places", "objects", "timeline", "goals", "lore", "magic", "research", "gallery" };

    public bool IsSupportedType(string type) => Types.Contains(type);

    public async Task<IReadOnlyList<StudioEntryDto>> ListAsync(Guid bookId, string type, CancellationToken ct = default)
    {
        var query = db.StoryStudioEntries.AsNoTracking().Where(x => x.BookId == bookId && x.Type == type.ToLower());
        var items = type.Equals("timeline", StringComparison.OrdinalIgnoreCase)
            ? await query.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt).ToListAsync(ct)
            : await query.OrderByDescending(x => x.UpdatedAt).ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<Result<StudioEntryDto>> CreateAsync(Guid bookId, string type, CreateStudioEntryDto request, CancellationToken ct = default)
    {
        if (!IsSupportedType(type)) return Result<StudioEntryDto>.Failure(Error.Custom("UNKNOWN_TYPE", "Unknown studio entry type."));
        if (string.IsNullOrWhiteSpace(request.Name)) return Result<StudioEntryDto>.Failure(Error.Custom("NAME_REQUIRED", "Name is required."));
        if (request.Image?.Length > 7_000_000) return Result<StudioEntryDto>.Failure(Error.Custom("IMAGE_TOO_LARGE", "Image is too large."));
        var item = StoryStudioEntry.Create(bookId, type.ToLower(), request.Name, request.Summary, request.Details,
            request.Motivation, request.Plot, request.Image, request.EventDate, request.Impact,
            SanitizeIds(request.CharacterIds), SanitizeIds(request.ObjectIds), SanitizeIds(request.PlaceIds), request.GoalTarget, request.GoalProgress ?? 0);
        if (type.Equals("timeline", StringComparison.OrdinalIgnoreCase))
            item.SetSortOrder(await db.StoryStudioEntries.CountAsync(x => x.BookId == bookId && x.Type == "timeline", ct));
        db.StoryStudioEntries.Add(item);
        await db.SaveChangesAsync(ct);
        return Result<StudioEntryDto>.Success(Map(item));
    }

    public async Task<Result<StudioEntryDto>> UpdateGoalProgressAsync(Guid bookId, Guid id, int progress, CancellationToken ct = default)
    {
        var goal = await db.StoryStudioEntries.SingleOrDefaultAsync(x => x.BookId == bookId && x.Id == id && x.Type == "goals", ct);
        if (goal is null) return Result<StudioEntryDto>.Failure(Error.NotFound);
        goal.SetGoalProgress(progress);
        await db.SaveChangesAsync(ct);
        return Result<StudioEntryDto>.Success(Map(goal));
    }

    public async Task<Result> DeleteEntryAsync(Guid bookId, Guid id, CancellationToken ct = default)
    {
        var item = await db.StoryStudioEntries.FirstOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (item is null) return Result.Failure(Error.NotFound);
        db.StoryStudioEntries.Remove(item);
        await db.StoryRelationships.Where(x => x.BookId == bookId && (x.FromEntryId == id || x.ToEntryId == id)).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string? SanitizeIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var ids = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => Guid.TryParse(x, out _)).Distinct().ToArray();
        return ids.Length == 0 ? null : string.Join(',', ids);
    }

    private static StudioEntryDto Map(StoryStudioEntry x) => new(x.Id, x.BookId, x.Type, x.Name, x.Summary, x.Details,
        x.Motivation, x.Plot, x.ImageData, x.CreatedAt, x.UpdatedAt, x.SortOrder, x.EventDate, x.Impact,
        x.RelatedCharacterIds, x.RelatedObjectIds, x.RelatedPlaceIds, x.GoalTarget, x.GoalProgress);
}
