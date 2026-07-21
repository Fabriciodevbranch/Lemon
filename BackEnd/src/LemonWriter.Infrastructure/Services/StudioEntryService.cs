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
        if (!type.Equals("gallery", StringComparison.OrdinalIgnoreCase)) return items.Select(x => Map(x)).ToList();
        var collectionIds = items.Where(x => x.CollectionId.HasValue).Select(x => x.CollectionId!.Value).Distinct().ToArray();
        var names = await db.StoryMediaCollections.AsNoTracking().Where(x => collectionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        return items.Select(x => Map(x, x.CollectionId is Guid id && names.TryGetValue(id, out var name) ? name : null)).ToList();
    }

    public async Task<Result<IReadOnlyList<StudioEntryDto>>> CreateGalleryBatchAsync(Guid bookId, CreateGalleryBatchDto request, CancellationToken ct = default)
    {
        if (request.Items.Count is < 2 or > 20)
            return Result<IReadOnlyList<StudioEntryDto>>.Failure(Error.Custom("INVALID_BATCH", "Select between 2 and 20 images."));
        if (request.Items.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            return Result<IReadOnlyList<StudioEntryDto>>.Failure(Error.Custom("NAME_REQUIRED", "Every image needs a name."));
        if (request.Items.Any(x => string.IsNullOrWhiteSpace(x.Image) || x.Image.Length > 7_000_000))
            return Result<IReadOnlyList<StudioEntryDto>>.Failure(Error.Custom("INVALID_IMAGE", "Every image is required and must be at most 5 MB."));
        if (request.Items.Sum(x => x.Image?.Length ?? 0) > 70_000_000)
            return Result<IReadOnlyList<StudioEntryDto>>.Failure(Error.Custom("BATCH_TOO_LARGE", "The selected images are too large as a group."));

        StoryMediaCollection? collection = null;
        if (!string.IsNullOrWhiteSpace(request.CollectionName))
        {
            if (request.CollectionName.Trim().Length > 200)
                return Result<IReadOnlyList<StudioEntryDto>>.Failure(Error.Custom("COLLECTION_NAME_TOO_LONG", "Collection name must be 200 characters or fewer."));
            collection = StoryMediaCollection.Create(bookId, request.CollectionName);
            db.StoryMediaCollections.Add(collection);
        }

        var items = request.Items.Select(x => StoryStudioEntry.Create(bookId, "gallery", x.Name, x.Summary, x.Details,
            null, null, x.Image, collectionId: collection?.Id)).ToList();
        db.StoryStudioEntries.AddRange(items);
        await db.SaveChangesAsync(ct);
        return Result<IReadOnlyList<StudioEntryDto>>.Success(items.Select(x => Map(x, collection?.Name)).ToList());
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

    public async Task<Result<StudioEntryDto>> UpdateMetadataAsync(Guid bookId, Guid id, UpdateStudioEntryMetadataDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<StudioEntryDto>.Failure(Error.Custom("NAME_REQUIRED", "Name is required."));
        if (request.Name.Trim().Length > 300 || request.Summary?.Length > 1000)
            return Result<StudioEntryDto>.Failure(Error.Custom("METADATA_TOO_LONG", "The media metadata is too long."));
        var item = await db.StoryStudioEntries.SingleOrDefaultAsync(x => x.BookId == bookId && x.Id == id && x.Type == "gallery", ct);
        if (item is null) return Result<StudioEntryDto>.Failure(Error.NotFound);
        item.UpdateMetadata(request.Name, request.Summary, request.Details);
        await db.SaveChangesAsync(ct);
        string? collectionName = null;
        if (item.CollectionId is Guid collectionId)
            collectionName = await db.StoryMediaCollections.Where(x => x.Id == collectionId).Select(x => x.Name).SingleOrDefaultAsync(ct);
        return Result<StudioEntryDto>.Success(Map(item, collectionName));
    }

    public async Task<Result> DeleteEntryAsync(Guid bookId, Guid id, CancellationToken ct = default)
    {
        var item = await db.StoryStudioEntries.FirstOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (item is null) return Result.Failure(Error.NotFound);
        StoryMediaCollection? emptyCollection = null;
        if (item.CollectionId is Guid collectionId &&
            !await db.StoryStudioEntries.AnyAsync(x => x.CollectionId == collectionId && x.Id != id, ct))
            emptyCollection = await db.StoryMediaCollections.FirstOrDefaultAsync(x => x.Id == collectionId && x.BookId == bookId, ct);
        db.StoryStudioEntries.Remove(item);
        if (emptyCollection is not null) db.StoryMediaCollections.Remove(emptyCollection);
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

    private static StudioEntryDto Map(StoryStudioEntry x, string? collectionName = null) => new(x.Id, x.BookId, x.Type, x.Name, x.Summary, x.Details,
        x.Motivation, x.Plot, x.ImageData, x.CreatedAt, x.UpdatedAt, x.SortOrder, x.EventDate, x.Impact,
        x.RelatedCharacterIds, x.RelatedObjectIds, x.RelatedPlaceIds, x.GoalTarget, x.GoalProgress, x.CollectionId, collectionName);
}
