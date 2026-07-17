using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Services;

public sealed class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly LemonDbContext _db;
    public ResourceAuthorizationService(LemonDbContext db) => _db = db;

    public Task<bool> OwnsBookAsync(Guid userId, Guid bookId, CancellationToken ct = default) =>
        _db.Books.AnyAsync(x => x.Id == bookId && x.AuthorId == userId, ct);

    public Task<bool> OwnsChapterAsync(Guid userId, Guid chapterId, CancellationToken ct = default) =>
        (from chapter in _db.Chapters join book in _db.Books on chapter.BookId equals book.Id
         where chapter.Id == chapterId && book.AuthorId == userId select chapter.Id).AnyAsync(ct);

    public Task<bool> OwnsDraftAsync(Guid userId, Guid draftId, CancellationToken ct = default) =>
        (from draft in _db.Drafts join chapter in _db.Chapters on draft.ChapterId equals chapter.Id
         join book in _db.Books on chapter.BookId equals book.Id
         where draft.Id == draftId && book.AuthorId == userId select draft.Id).AnyAsync(ct);

    public Task<bool> OwnsSnapshotAsync(Guid userId, Guid snapshotId, CancellationToken ct = default) =>
        (from snapshot in _db.Snapshots join chapter in _db.Chapters on snapshot.ChapterId equals chapter.Id
         join book in _db.Books on chapter.BookId equals book.Id
         where snapshot.Id == snapshotId && book.AuthorId == userId select snapshot.Id).AnyAsync(ct);
}
