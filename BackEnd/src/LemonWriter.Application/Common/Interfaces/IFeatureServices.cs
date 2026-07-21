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
    Task<Result> DeleteRelationshipAsync(Guid bookId, Guid id, CancellationToken ct = default);
}

public interface IStoryMetricsService
{
    Task<StoryMetricsDto> GetMetricsAsync(Guid bookId, Guid userId, CancellationToken ct = default);
}
