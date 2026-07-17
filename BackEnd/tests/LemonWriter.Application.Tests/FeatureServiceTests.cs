using FluentAssertions;
using LemonWriter.Application.Common.DTOs;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using LemonWriter.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LemonWriter.Application.Tests;

public sealed class FeatureServiceTests
{
    [Fact]
    public async Task Library_stats_are_calculated_behind_the_application_port()
    {
        await using var db = CreateDb();
        var book = Book.Create("Book", "", Guid.NewGuid(), BookMetadata.Create("Writer"));
        var chapter = Chapter.Create(book.Id, "One", 0);
        chapter.Update("One", "one two three four", 0);
        db.AddRange(book, chapter);
        await db.SaveChangesAsync();

        var stats = await new LibraryQueryService(db).GetBookStatsAsync([book.Id]);

        stats[book.Id].ChapterCount.Should().Be(1);
        stats[book.Id].WordCount.Should().Be(4);
    }

    [Fact]
    public async Task Privacy_preference_is_read_and_updated_through_its_port()
    {
        await using var db = CreateDb();
        var user = User.Create("writer@example.com", "Writer");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new PrivacyPreferenceService(db);

        (await service.GetStoryMetricsEnabledAsync(user.Id)).Should().BeFalse();
        (await service.SetStoryMetricsEnabledAsync(user.Id, true)).IsSuccess.Should().BeTrue();
        (await service.GetStoryMetricsEnabledAsync(user.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Studio_use_case_validates_and_persists_entries()
    {
        await using var db = CreateDb();
        var service = new StudioEntryService(db);
        var bookId = Guid.NewGuid();

        var invalid = await service.CreateAsync(bookId, "timeline", new CreateStudioEntryDto("", null, null, null, null, null, null, null, null, null, null, null, null));
        var valid = await service.CreateAsync(bookId, "timeline", new CreateStudioEntryDto("Inciting incident", "Everything changes", null, null, null, null, "Day 1", "Begins the journey", null, null, null, null, null));

        invalid.IsFailure.Should().BeTrue();
        valid.IsSuccess.Should().BeTrue();
        (await service.ListAsync(bookId, "timeline")).Should().ContainSingle(x => x.Name == "Inciting incident");
    }

    [Fact]
    public async Task Timeline_slice_reorders_only_valid_events()
    {
        await using var db = CreateDb();
        var bookId = Guid.NewGuid();
        var first = StoryStudioEntry.Create(bookId, "timeline", "First", null, null, null, null, null);
        var second = StoryStudioEntry.Create(bookId, "timeline", "Second", null, null, null, null, null);
        db.StoryStudioEntries.AddRange(first, second);
        await db.SaveChangesAsync();
        var timeline = new TimelineService(db);

        (await timeline.ReorderAsync(bookId, [second.Id, first.Id])).IsSuccess.Should().BeTrue();
        (await timeline.ReorderAsync(bookId, [first.Id, first.Id])).IsFailure.Should().BeTrue();
        var ordered = await new StudioEntryService(db).ListAsync(bookId, "timeline");
        ordered.Select(x => x.Id).Should().ContainInOrder(second.Id, first.Id);
    }

    [Fact]
    public async Task Relationship_slice_accepts_only_characters_from_the_same_book()
    {
        await using var db = CreateDb();
        var bookId = Guid.NewGuid();
        var one = StoryStudioEntry.Create(bookId, "characters", "One", null, null, null, null, null);
        var two = StoryStudioEntry.Create(bookId, "characters", "Two", null, null, null, null, null);
        var outsider = StoryStudioEntry.Create(Guid.NewGuid(), "characters", "Outsider", null, null, null, null, null);
        db.StoryStudioEntries.AddRange(one, two, outsider);
        await db.SaveChangesAsync();
        var relationships = new RelationshipService(db);

        (await relationships.CreateRelationshipAsync(bookId, new(one.Id, two.Id, "trusts", "positive"))).IsSuccess.Should().BeTrue();
        (await relationships.CreateRelationshipAsync(bookId, new(one.Id, outsider.Id, null, null))).IsFailure.Should().BeTrue();
        (await relationships.ListRelationshipsAsync(bookId)).Should().ContainSingle();
    }

    [Fact]
    public async Task Metrics_slice_honors_the_users_opt_in_preference()
    {
        await using var db = CreateDb();
        var user = User.Create("metrics@example.com", "Writer");
        var bookId = Guid.NewGuid();
        db.Users.Add(user);
        db.StoryStudioEntries.Add(StoryStudioEntry.Create(bookId, "characters", "Hero", "Lead", "Complete", "Save home", null, null));
        await db.SaveChangesAsync();
        var metrics = new StoryMetricsService(db);

        (await metrics.GetMetricsAsync(bookId, user.Id)).Enabled.Should().BeFalse();
        user.SetStoryMetrics(true);
        await db.SaveChangesAsync();
        var enabled = await metrics.GetMetricsAsync(bookId, user.Id);
        enabled.Enabled.Should().BeTrue();
        enabled.Characters.Should().Be(1);
    }

    private static LemonDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LemonDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new LemonDbContext(options);
    }
}
