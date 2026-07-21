using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class RelationshipService(LemonDbContext db) : IRelationshipService
{
    private static readonly HashSet<string> Types = ["Family", "Friend", "Ally", "Rival", "Enemy", "Romantic", "Mentor", "Student", "Colleague", "Acquaintance", "Other"];
    private static readonly HashSet<string> Tones = ["Positive", "Negative", "Mixed", "Neutral", "Unknown", "positive", "negative", "neutral"];
    private static readonly HashSet<string> Statuses = ["Active", "Broken", "Past", "Hidden", "Unknown"];
    public async Task<IReadOnlyList<StoryRelationshipDto>> ListRelationshipsAsync(Guid bookId, CancellationToken ct = default) =>
        await db.StoryRelationships.AsNoTracking().Where(x => x.BookId == bookId).OrderByDescending(x => x.CreatedAt)
            .Select(x => new StoryRelationshipDto(x.Id, x.FromEntryId, x.ToEntryId, x.Label, x.Tone, x.RelationshipType, x.Description, x.Status)).ToListAsync(ct);

    public async Task<Result<StoryRelationshipDto>> CreateRelationshipAsync(Guid bookId, CreateStoryRelationshipDto request, CancellationToken ct = default)
    {
        if (request.From == request.To)
            return Result<StoryRelationshipDto>.Failure(Error.Custom("SAME_CHARACTER", "Choose two different characters."));
        var count = await db.StoryStudioEntries.CountAsync(x => x.BookId == bookId && x.Type == "characters" && (x.Id == request.From || x.Id == request.To), ct);
        if (count != 2)
            return Result<StoryRelationshipDto>.Failure(Error.Custom("INVALID_CHARACTERS", "Both characters must belong to this book."));
        var validation = Validate(request); if (validation is not null) return Result<StoryRelationshipDto>.Failure(validation);
        if (await IsDuplicate(bookId, null, request, ct)) return Result<StoryRelationshipDto>.Failure(Error.Custom("DUPLICATE_RELATIONSHIP", "This exact relationship already exists."));
        var relation = StoryRelationship.Create(bookId, request.From, request.To, request.Label, request.Tone,
            request.RelationshipType, request.Description, request.Status);
        db.StoryRelationships.Add(relation);
        await db.SaveChangesAsync(ct);
        return Result<StoryRelationshipDto>.Success(Map(relation));
    }

    public async Task<Result<StoryRelationshipDto>> UpdateRelationshipAsync(Guid bookId, Guid id, CreateStoryRelationshipDto request, CancellationToken ct = default)
    {
        var relation = await db.StoryRelationships.SingleOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (relation is null) return Result<StoryRelationshipDto>.Failure(Error.NotFound);
        if (relation.FromEntryId != request.From || relation.ToEntryId != request.To)
            return Result<StoryRelationshipDto>.Failure(Error.Custom("DIRECTION_IMMUTABLE", "Create a new relationship to change its direction."));
        var validation = Validate(request); if (validation is not null) return Result<StoryRelationshipDto>.Failure(validation);
        if (await IsDuplicate(bookId, id, request, ct)) return Result<StoryRelationshipDto>.Failure(Error.Custom("DUPLICATE_RELATIONSHIP", "This exact relationship already exists."));
        relation.Update(request.RelationshipType, request.Label, request.Description, request.Tone, request.Status);
        await db.SaveChangesAsync(ct); return Result<StoryRelationshipDto>.Success(Map(relation));
    }

    public async Task<Result> DeleteRelationshipAsync(Guid bookId, Guid id, CancellationToken ct = default)
    {
        var relation = await db.StoryRelationships.FirstOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (relation is null) return Result.Failure(Error.NotFound);
        db.Remove(relation);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static Error? Validate(CreateStoryRelationshipDto request) => !Types.Contains(request.RelationshipType) ? Error.Custom("INVALID_RELATIONSHIP_TYPE", "Choose a valid relationship type.")
        : request.Tone is not null && !Tones.Contains(request.Tone) ? Error.Custom("INVALID_TONE", "Choose a valid emotional tone.")
        : request.Status is not null && !Statuses.Contains(request.Status) ? Error.Custom("INVALID_STATUS", "Choose a valid relationship status.") : null;
    private Task<bool> IsDuplicate(Guid bookId, Guid? exceptId, CreateStoryRelationshipDto request, CancellationToken ct) =>
        db.StoryRelationships.AnyAsync(x => x.BookId == bookId && x.Id != exceptId && x.FromEntryId == request.From && x.ToEntryId == request.To &&
            x.RelationshipType == request.RelationshipType && x.Label == (string.IsNullOrWhiteSpace(request.Label) ? request.RelationshipType : request.Label.Trim()), ct);
    private static StoryRelationshipDto Map(StoryRelationship x) => new(x.Id, x.FromEntryId, x.ToEntryId, x.Label, x.Tone, x.RelationshipType, x.Description, x.Status);
}
