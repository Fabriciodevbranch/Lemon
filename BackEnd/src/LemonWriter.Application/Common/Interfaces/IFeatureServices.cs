using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;

namespace LemonWriter.Application.Common.Interfaces;

public interface ILibraryQueryService
{
    Task<IReadOnlyDictionary<Guid, LibraryBookStatsDto>> GetBookStatsAsync(IEnumerable<Guid> bookIds, CancellationToken ct = default);
}

public interface IPrivacyPreferenceService
{
    Task<bool?> GetStoryMetricsEnabledAsync(Guid userId, CancellationToken ct = default);
    Task<Result> SetStoryMetricsEnabledAsync(Guid userId, bool enabled, CancellationToken ct = default);
}

public interface IAppearancePreferenceService
{
    Task<AppearancePreferenceDto?> GetAsync(Guid userId, CancellationToken ct = default);
    Task<Result> SetAsync(Guid userId, AppearancePreferenceDto preference, CancellationToken ct = default);
}

public interface IStudioEntryService
{
    bool IsSupportedType(string type);
    Task<IReadOnlyList<StudioEntryDto>> ListAsync(Guid bookId, string type, CancellationToken ct = default);
    Task<Result<StudioEntryDto>> CreateAsync(Guid bookId, string type, CreateStudioEntryDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StudioEntryDto>>> CreateGalleryBatchAsync(Guid bookId, CreateGalleryBatchDto request, CancellationToken ct = default);
    Task<Result<StudioEntryDto>> UpdateMetadataAsync(Guid bookId, Guid id, UpdateStudioEntryMetadataDto request, CancellationToken ct = default);
    Task<Result<StudioEntryDto>> UpdateCharacterProfileAsync(Guid bookId, Guid id, UpdateCharacterProfileDto request, CancellationToken ct = default);
    Task<Result<StudioEntryDto>> UpdateGoalProgressAsync(Guid bookId, Guid id, int progress, CancellationToken ct = default);
    Task<Result> DeleteEntryAsync(Guid bookId, Guid id, CancellationToken ct = default);
}

public interface ITimelineService
{
    Task<Result> ReorderAsync(Guid bookId, IReadOnlyList<Guid> ids, CancellationToken ct = default);
}

public interface IRelationshipService
{
    Task<IReadOnlyList<StoryRelationshipDto>> ListRelationshipsAsync(Guid bookId, CancellationToken ct = default);
    Task<Result<StoryRelationshipDto>> CreateRelationshipAsync(Guid bookId, CreateStoryRelationshipDto request, CancellationToken ct = default);
    Task<Result<StoryRelationshipDto>> UpdateRelationshipAsync(Guid bookId, Guid id, CreateStoryRelationshipDto request, CancellationToken ct = default);
    Task<Result> DeleteRelationshipAsync(Guid bookId, Guid id, CancellationToken ct = default);
}

public interface ICharacterConnectionsService
{
    Task<Result<IReadOnlyList<CharacterMediaReferenceDto>>> ListMediaAsync(Guid bookId, Guid characterId, CancellationToken ct = default);
    Task<Result<CharacterMediaReferenceDto>> SaveMediaAsync(Guid bookId, Guid characterId, Guid? id, SaveCharacterMediaReferenceDto request, CancellationToken ct = default);
    Task<Result> DeleteMediaAsync(Guid bookId, Guid characterId, Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CharacterTimelineReferenceDto>>> ListTimelineAsync(Guid bookId, Guid characterId, CancellationToken ct = default);
    Task<Result<CharacterTimelineReferenceDto>> SaveTimelineAsync(Guid bookId, Guid characterId, Guid? id, SaveCharacterTimelineReferenceDto request, CancellationToken ct = default);
    Task<Result> DeleteTimelineAsync(Guid bookId, Guid characterId, Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CharacterAttributeDto>>> ListAttributesAsync(Guid bookId, Guid characterId, CancellationToken ct = default);
    Task<Result<CharacterAttributeDto>> SaveAttributeAsync(Guid bookId, Guid characterId, Guid? id, SaveCharacterAttributeDto request, CancellationToken ct = default);
    Task<Result> DeleteAttributeAsync(Guid bookId, Guid characterId, Guid id, CancellationToken ct = default);
}

public interface IStoryMetricsService
{
    Task<StoryMetricsDto> GetMetricsAsync(Guid bookId, Guid userId, CancellationToken ct = default);
}
