using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class RelationshipService(LemonDbContext db) : IRelationshipService
{
    public async Task<IReadOnlyList<StoryRelationshipDto>> ListRelationshipsAsync(Guid bookId, CancellationToken ct = default) =>
        await db.StoryRelationships.AsNoTracking().Where(x => x.BookId == bookId).OrderByDescending(x => x.CreatedAt)
            .Select(x => new StoryRelationshipDto(x.Id, x.FromEntryId, x.ToEntryId, x.Label, x.Tone)).ToListAsync(ct);

    public async Task<Result<StoryRelationshipDto>> CreateRelationshipAsync(Guid bookId, CreateStoryRelationshipDto request, CancellationToken ct = default)
    {
        if (request.From == request.To)
            return Result<StoryRelationshipDto>.Failure(Error.Custom("SAME_CHARACTER", "Choose two different characters."));
        var count = await db.StoryStudioEntries.CountAsync(x => x.BookId == bookId && x.Type == "characters" && (x.Id == request.From || x.Id == request.To), ct);
        if (count != 2)
            return Result<StoryRelationshipDto>.Failure(Error.Custom("INVALID_CHARACTERS", "Both characters must belong to this book."));
        var relation = StoryRelationship.Create(bookId, request.From, request.To, request.Label, request.Tone);
        db.StoryRelationships.Add(relation);
        await db.SaveChangesAsync(ct);
        return Result<StoryRelationshipDto>.Success(new(relation.Id, relation.FromEntryId, relation.ToEntryId, relation.Label, relation.Tone));
    }

    public async Task<Result> DeleteRelationshipAsync(Guid bookId, Guid id, CancellationToken ct = default)
    {
        var relation = await db.StoryRelationships.FirstOrDefaultAsync(x => x.BookId == bookId && x.Id == id, ct);
        if (relation is null) return Result.Failure(Error.NotFound);
        db.Remove(relation);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
