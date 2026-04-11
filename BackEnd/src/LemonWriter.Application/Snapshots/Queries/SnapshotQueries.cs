using LemonWriter.Application.Common.DTOs;
using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using MediatR;

namespace LemonWriter.Application.Snapshots.Queries;

public record GetSnapshotByIdQuery(Guid Id) : IRequest<Result<SnapshotDto>>;

public class GetSnapshotByIdQueryHandler : IRequestHandler<GetSnapshotByIdQuery, Result<SnapshotDto>>
{
    private readonly ISnapshotRepository _snapshotRepository;

    public GetSnapshotByIdQueryHandler(ISnapshotRepository snapshotRepository) => _snapshotRepository = snapshotRepository;

    public async Task<Result<SnapshotDto>> Handle(GetSnapshotByIdQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.GetByIdAsync(request.Id, cancellationToken);
        if (snapshot is null)
            return Result<SnapshotDto>.Failure(Error.NotFound);

        return Result<SnapshotDto>.Success(new SnapshotDto(
            snapshot.Id, snapshot.ChapterId, snapshot.Content,
            snapshot.SnapshotMessage, snapshot.CreatedAt, snapshot.AuthorId, snapshot.ParentSnapshotId));
    }
}

public record CompareSnapshotsQuery(Guid BaseSnapshotId, Guid CompareSnapshotId) : IRequest<Result<SnapshotComparisonDto>>;

public class CompareSnapshotsQueryHandler : IRequestHandler<CompareSnapshotsQuery, Result<SnapshotComparisonDto>>
{
    private readonly ISnapshotRepository _snapshotRepository;

    public CompareSnapshotsQueryHandler(ISnapshotRepository snapshotRepository) => _snapshotRepository = snapshotRepository;

    public async Task<Result<SnapshotComparisonDto>> Handle(CompareSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var baseSnapshot = await _snapshotRepository.GetByIdAsync(request.BaseSnapshotId, cancellationToken);
        var compareSnapshot = await _snapshotRepository.GetByIdAsync(request.CompareSnapshotId, cancellationToken);

        if (baseSnapshot is null || compareSnapshot is null)
            return Result<SnapshotComparisonDto>.Failure(Error.NotFound);

        var baseLines = baseSnapshot.Content.Split('\n');
        var compareLines = compareSnapshot.Content.Split('\n');
        var differences = new List<string>();

        var maxLines = Math.Max(baseLines.Length, compareLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            var baseLine = i < baseLines.Length ? baseLines[i] : string.Empty;
            var compareLine = i < compareLines.Length ? compareLines[i] : string.Empty;
            if (baseLine != compareLine)
                differences.Add($"Line {i + 1}: [{baseLine}] -> [{compareLine}]");
        }

        var baseDto = new SnapshotDto(baseSnapshot.Id, baseSnapshot.ChapterId, baseSnapshot.Content,
            baseSnapshot.SnapshotMessage, baseSnapshot.CreatedAt, baseSnapshot.AuthorId, baseSnapshot.ParentSnapshotId);
        var compareDto = new SnapshotDto(compareSnapshot.Id, compareSnapshot.ChapterId, compareSnapshot.Content,
            compareSnapshot.SnapshotMessage, compareSnapshot.CreatedAt, compareSnapshot.AuthorId, compareSnapshot.ParentSnapshotId);

        return Result<SnapshotComparisonDto>.Success(new SnapshotComparisonDto(baseDto, compareDto, differences));
    }
}
