using FluentAssertions;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using LemonWriter.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LemonWriter.Application.Tests;

public sealed class ResourceAuthorizationServiceTests
{
    [Fact]
    public async Task Ownership_chain_allows_owner_and_rejects_another_user()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var book = Book.Create("Private book", "", ownerId, BookMetadata.Create("Writer"));
        var chapter = Chapter.Create(book.Id, "Chapter", 0);
        var draft = Draft.Create(chapter.Id, "Draft");
        var snapshot = Snapshot.Create(chapter.Id, "content", "checkpoint", ownerId);
        db.AddRange(book, chapter, draft, snapshot);
        await db.SaveChangesAsync();
        var authorization = new ResourceAuthorizationService(db);

        (await authorization.OwnsBookAsync(ownerId, book.Id)).Should().BeTrue();
        (await authorization.OwnsChapterAsync(ownerId, chapter.Id)).Should().BeTrue();
        (await authorization.OwnsDraftAsync(ownerId, draft.Id)).Should().BeTrue();
        (await authorization.OwnsSnapshotAsync(ownerId, snapshot.Id)).Should().BeTrue();

        (await authorization.OwnsBookAsync(attackerId, book.Id)).Should().BeFalse();
        (await authorization.OwnsChapterAsync(attackerId, chapter.Id)).Should().BeFalse();
        (await authorization.OwnsDraftAsync(attackerId, draft.Id)).Should().BeFalse();
        (await authorization.OwnsSnapshotAsync(attackerId, snapshot.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Unknown_resources_are_not_owned()
    {
        await using var db = CreateDb();
        var authorization = new ResourceAuthorizationService(db);
        var userId = Guid.NewGuid();

        (await authorization.OwnsBookAsync(userId, Guid.NewGuid())).Should().BeFalse();
        (await authorization.OwnsChapterAsync(userId, Guid.NewGuid())).Should().BeFalse();
        (await authorization.OwnsDraftAsync(userId, Guid.NewGuid())).Should().BeFalse();
        (await authorization.OwnsSnapshotAsync(userId, Guid.NewGuid())).Should().BeFalse();
    }

    private static LemonDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LemonDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new LemonDbContext(options);
    }
}
