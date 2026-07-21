using System.Globalization;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class CharacterConnectionsService(LemonDbContext db) : ICharacterConnectionsService
{
    private static readonly HashSet<string> MediaRoles = ["Portrait", "Appearance reference", "Outfit", "Mood", "Location association", "Symbol", "Other"];
    private static readonly HashSet<string> EventRoles = ["Participant", "Witness", "Cause", "Target", "Mentioned", "POV", "Other"];
    private static readonly HashSet<string> ValueTypes = ["ShortText", "LongText", "Number", "Boolean", "Date", "SingleSelect"];

    public async Task<Result<IReadOnlyList<CharacterMediaReferenceDto>>> ListMediaAsync(Guid bookId, Guid characterId, CancellationToken ct = default)
    {
        if (!await IsCharacter(bookId, characterId, ct)) return Result<IReadOnlyList<CharacterMediaReferenceDto>>.Failure(Error.NotFound);
        var rows = await (from link in db.CharacterMediaReferences.AsNoTracking()
            join media in db.StoryStudioEntries.AsNoTracking() on link.MediaId equals media.Id
            join collection in db.StoryMediaCollections.AsNoTracking() on media.CollectionId equals collection.Id into collections
            from collection in collections.DefaultIfEmpty()
            where link.BookId == bookId && link.CharacterId == characterId
            orderby link.DisplayOrder, media.Name
            select new CharacterMediaReferenceDto(link.Id, characterId, media.Id, link.Role, link.DisplayOrder, media.Name,
                media.ImageData, media.CollectionId, collection == null ? null : collection.Name)).ToListAsync(ct);
        return Result<IReadOnlyList<CharacterMediaReferenceDto>>.Success(rows);
    }

    public async Task<Result<CharacterMediaReferenceDto>> SaveMediaAsync(Guid bookId, Guid characterId, Guid? id, SaveCharacterMediaReferenceDto request, CancellationToken ct = default)
    {
        if (!MediaRoles.Contains(request.Role)) return Result<CharacterMediaReferenceDto>.Failure(Error.Custom("INVALID_MEDIA_ROLE", "Choose a valid media role."));
        if (!await IsCharacter(bookId, characterId, ct)) return Result<CharacterMediaReferenceDto>.Failure(Error.NotFound);
        var media = await db.StoryStudioEntries.SingleOrDefaultAsync(x => x.BookId == bookId && x.Id == request.MediaId && x.Type == "gallery", ct);
        if (media is null) return Result<CharacterMediaReferenceDto>.Failure(Error.Custom("INVALID_MEDIA", "The media must belong to this book."));
        CharacterMediaReference link;
        if (id is Guid linkId) { link = await db.CharacterMediaReferences.SingleOrDefaultAsync(x => x.Id == linkId && x.BookId == bookId && x.CharacterId == characterId, ct) ?? null!; if (link is null) return Result<CharacterMediaReferenceDto>.Failure(Error.NotFound); link.Update(request.Role, request.DisplayOrder); }
        else { if (await db.CharacterMediaReferences.AnyAsync(x => x.BookId == bookId && x.CharacterId == characterId && x.MediaId == request.MediaId, ct)) return Result<CharacterMediaReferenceDto>.Failure(Error.Custom("MEDIA_ALREADY_LINKED", "This media is already linked.")); link = CharacterMediaReference.Create(bookId, characterId, request.MediaId, request.Role, request.DisplayOrder); db.Add(link); }
        await db.SaveChangesAsync(ct);
        var collectionName = media.CollectionId is Guid collectionId ? await db.StoryMediaCollections.Where(x => x.Id == collectionId).Select(x => x.Name).SingleOrDefaultAsync(ct) : null;
        return Result<CharacterMediaReferenceDto>.Success(new(link.Id, characterId, media.Id, link.Role, link.DisplayOrder, media.Name, media.ImageData, media.CollectionId, collectionName));
    }
    public async Task<Result> DeleteMediaAsync(Guid bookId, Guid characterId, Guid id, CancellationToken ct = default) => await Delete(db.CharacterMediaReferences, x => x.BookId == bookId && x.CharacterId == characterId && x.Id == id, ct);

    public async Task<Result<IReadOnlyList<CharacterTimelineReferenceDto>>> ListTimelineAsync(Guid bookId, Guid characterId, CancellationToken ct = default)
    {
        if (!await IsCharacter(bookId, characterId, ct)) return Result<IReadOnlyList<CharacterTimelineReferenceDto>>.Failure(Error.NotFound);
        var rows = await (from link in db.CharacterTimelineReferences.AsNoTracking() join item in db.StoryStudioEntries.AsNoTracking() on link.EventId equals item.Id
            where link.BookId == bookId && link.CharacterId == characterId orderby item.SortOrder, item.CreatedAt
            select new CharacterTimelineReferenceDto(link.Id, characterId, item.Id, link.Role, link.Note, item.Name, item.EventDate, item.SortOrder, false)).ToListAsync(ct);
        var linked = rows.Select(x => x.EventId).ToHashSet();
        var legacy = await db.StoryStudioEntries.AsNoTracking().Where(x => x.BookId == bookId && x.Type == "timeline" && x.RelatedCharacterIds != null).OrderBy(x => x.SortOrder).ToListAsync(ct);
        rows.AddRange(legacy.Where(x => !linked.Contains(x.Id) && x.RelatedCharacterIds!.Split(',').Contains(characterId.ToString()))
            .Select(x => new CharacterTimelineReferenceDto(Guid.Empty, characterId, x.Id, "Participant", null, x.Name, x.EventDate, x.SortOrder, true)));
        return Result<IReadOnlyList<CharacterTimelineReferenceDto>>.Success(rows.OrderBy(x => x.SortOrder).ToList());
    }
    public async Task<Result<CharacterTimelineReferenceDto>> SaveTimelineAsync(Guid bookId, Guid characterId, Guid? id, SaveCharacterTimelineReferenceDto request, CancellationToken ct = default)
    {
        if (!EventRoles.Contains(request.Role)) return Result<CharacterTimelineReferenceDto>.Failure(Error.Custom("INVALID_EVENT_ROLE", "Choose a valid involvement role."));
        if (!await IsCharacter(bookId, characterId, ct)) return Result<CharacterTimelineReferenceDto>.Failure(Error.NotFound);
        var item = await db.StoryStudioEntries.SingleOrDefaultAsync(x => x.BookId == bookId && x.Id == request.EventId && x.Type == "timeline", ct);
        if (item is null) return Result<CharacterTimelineReferenceDto>.Failure(Error.Custom("INVALID_EVENT", "The event must belong to this book."));
        CharacterTimelineReference link;
        if (id is Guid linkId) { link = await db.CharacterTimelineReferences.SingleOrDefaultAsync(x => x.Id == linkId && x.BookId == bookId && x.CharacterId == characterId, ct) ?? null!; if (link is null) return Result<CharacterTimelineReferenceDto>.Failure(Error.NotFound); link.Update(request.Role, request.Note); }
        else { if (await db.CharacterTimelineReferences.AnyAsync(x => x.BookId == bookId && x.CharacterId == characterId && x.EventId == request.EventId, ct)) return Result<CharacterTimelineReferenceDto>.Failure(Error.Custom("EVENT_ALREADY_LINKED", "This event is already linked.")); link = CharacterTimelineReference.Create(bookId, characterId, request.EventId, request.Role, request.Note); db.Add(link); }
        await db.SaveChangesAsync(ct); return Result<CharacterTimelineReferenceDto>.Success(new(link.Id, characterId, item.Id, link.Role, link.Note, item.Name, item.EventDate, item.SortOrder));
    }
    public async Task<Result> DeleteTimelineAsync(Guid bookId, Guid characterId, Guid id, CancellationToken ct = default) => await Delete(db.CharacterTimelineReferences, x => x.BookId == bookId && x.CharacterId == characterId && x.Id == id, ct);

    public async Task<Result<IReadOnlyList<CharacterAttributeDto>>> ListAttributesAsync(Guid bookId, Guid characterId, CancellationToken ct = default)
    {
        if (!await IsCharacter(bookId, characterId, ct)) return Result<IReadOnlyList<CharacterAttributeDto>>.Failure(Error.NotFound);
        var attrs = await db.CharacterCustomAttributes.AsNoTracking().Where(x => x.BookId == bookId && x.CharacterId == characterId).OrderBy(x => x.GroupName).ThenBy(x => x.DisplayOrder).ToListAsync(ct);
        var ids = attrs.Select(x => x.Id).ToArray(); var options = await db.CharacterAttributeOptions.AsNoTracking().Where(x => ids.Contains(x.AttributeId)).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        return Result<IReadOnlyList<CharacterAttributeDto>>.Success(attrs.Select(x => Map(x, options.Where(o => o.AttributeId == x.Id).Select(o => o.Value).ToList())).ToList());
    }
    public async Task<Result<CharacterAttributeDto>> SaveAttributeAsync(Guid bookId, Guid characterId, Guid? id, SaveCharacterAttributeDto request, CancellationToken ct = default)
    {
        var error = ValidateAttribute(request); if (error is not null) return Result<CharacterAttributeDto>.Failure(error);
        if (!await IsCharacter(bookId, characterId, ct)) return Result<CharacterAttributeDto>.Failure(Error.NotFound);
        var normalizedGroup = request.GroupName?.Trim() ?? string.Empty; var normalizedLabel = request.Label.Trim();
        if (await db.CharacterCustomAttributes.AnyAsync(x => x.BookId == bookId && x.CharacterId == characterId && x.Id != id && x.Label.ToLower() == normalizedLabel.ToLower() && (x.GroupName ?? "").ToLower() == normalizedGroup.ToLower(), ct))
            return Result<CharacterAttributeDto>.Failure(Error.Custom("DUPLICATE_ATTRIBUTE", "An attribute with this label already exists in the group."));
        CharacterCustomAttribute attr;
        if (id is Guid attrId) { attr = await db.CharacterCustomAttributes.SingleOrDefaultAsync(x => x.Id == attrId && x.BookId == bookId && x.CharacterId == characterId, ct) ?? null!; if (attr is null) return Result<CharacterAttributeDto>.Failure(Error.NotFound); attr.Update(request.Label, request.ValueType, request.Value, request.GroupName, request.DisplayOrder); db.CharacterAttributeOptions.RemoveRange(db.CharacterAttributeOptions.Where(x => x.AttributeId == attr.Id)); }
        else { attr = CharacterCustomAttribute.Create(bookId, characterId, request.Label, request.ValueType, request.Value, request.GroupName, request.DisplayOrder); db.Add(attr); }
        var values = request.ValueType == "SingleSelect" ? request.Options!.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList() : [];
        db.CharacterAttributeOptions.AddRange(values.Select((value, index) => CharacterAttributeOption.Create(attr.Id, value, index)));
        await db.SaveChangesAsync(ct); return Result<CharacterAttributeDto>.Success(Map(attr, values));
    }
    public async Task<Result> DeleteAttributeAsync(Guid bookId, Guid characterId, Guid id, CancellationToken ct = default)
    { db.CharacterAttributeOptions.RemoveRange(db.CharacterAttributeOptions.Where(x => x.AttributeId == id)); return await Delete(db.CharacterCustomAttributes, x => x.BookId == bookId && x.CharacterId == characterId && x.Id == id, ct); }

    private Task<bool> IsCharacter(Guid bookId, Guid id, CancellationToken ct) => db.StoryStudioEntries.AnyAsync(x => x.BookId == bookId && x.Id == id && x.Type == "characters", ct);
    private static Error? ValidateAttribute(SaveCharacterAttributeDto r)
    {
        if (string.IsNullOrWhiteSpace(r.Label)) return Error.Custom("LABEL_REQUIRED", "Attribute label is required.");
        if (!ValueTypes.Contains(r.ValueType)) return Error.Custom("INVALID_VALUE_TYPE", "Choose a valid value type.");
        if (r.ValueType == "Number" && !decimal.TryParse(r.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)) return Error.Custom("INVALID_NUMBER", "Enter a numeric value.");
        if (r.ValueType == "Boolean" && r.Value is not "true" and not "false") return Error.Custom("INVALID_BOOLEAN", "Choose true or false.");
        if (r.ValueType == "Date" && !DateOnly.TryParse(r.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return Error.Custom("INVALID_DATE", "Enter a valid date.");
        if (r.ValueType == "SingleSelect" && (r.Options is null || r.Options.Count == 0 || !r.Options.Contains(r.Value))) return Error.Custom("INVALID_OPTION", "Choose a value from the configured options.");
        return null;
    }
    private static CharacterAttributeDto Map(CharacterCustomAttribute x, IReadOnlyList<string> options) => new(x.Id, x.CharacterId, x.Label, x.ValueType, x.Value, x.GroupName, x.DisplayOrder, options);
    private async Task<Result> Delete<T>(DbSet<T> set, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken ct) where T : class
    { var row = await set.SingleOrDefaultAsync(predicate, ct); if (row is null) return Result.Failure(Error.NotFound); set.Remove(row); await db.SaveChangesAsync(ct); return Result.Success(); }
}
