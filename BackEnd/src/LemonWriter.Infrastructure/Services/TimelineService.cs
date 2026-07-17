using LemonWriter.Application.Common.Errors;
using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class TimelineService(LemonDbContext db) : ITimelineService
{
    public async Task<Result> ReorderAsync(Guid bookId, IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Distinct().Count() != ids.Count)
            return Result.Failure(Error.Custom("INVALID_TIMELINE", "The timeline order contains duplicate events."));
        var events = await db.StoryStudioEntries
            .Where(x => x.BookId == bookId && x.Type == "timeline" && ids.Contains(x.Id)).ToListAsync(ct);
        if (events.Count != ids.Count)
            return Result.Failure(Error.Custom("INVALID_TIMELINE", "The timeline order contains invalid events."));
        var byId = events.ToDictionary(x => x.Id);
        for (var index = 0; index < ids.Count; index++) byId[ids[index]].SetSortOrder(index);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
