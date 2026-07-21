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
    public void Character_connection_model_declares_database_foreign_keys()
    {
        using var db = CreateDb();

        ForeignKeyProperties<CharacterMediaReference>(db).Should().BeEquivalentTo("BookId", "CharacterId", "MediaId");
        ForeignKeyProperties<CharacterTimelineReference>(db).Should().BeEquivalentTo("BookId", "CharacterId", "EventId");
        ForeignKeyProperties<CharacterCustomAttribute>(db).Should().BeEquivalentTo("BookId", "CharacterId");
        ForeignKeyProperties<CharacterAttributeOption>(db).Should().BeEquivalentTo("AttributeId");
        ForeignKeyProperties<StoryRelationship>(db).Should().BeEquivalentTo("BookId", "FromEntryId", "ToEntryId");
        ForeignKeyProperties<StoryStudioEntry>(db).Should().Contain("PortraitMediaId");
    }

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
    public async Task Appearance_preference_is_persisted_per_user_and_validated()
    {
        await using var db = CreateDb();
        var user = User.Create("theme@example.com", "Writer");
        db.Users.Add(user); await db.SaveChangesAsync();
        var service = new AppearancePreferenceService(db);
        var preference = new AppearancePreferenceDto("cozy-night", new Dictionary<string, string>
            { ["--custom-primary-color"] = "#112233" });

        (await service.SetAsync(user.Id, preference)).IsSuccess.Should().BeTrue();
        var reloaded = await service.GetAsync(user.Id);
        reloaded!.Theme.Should().Be("cozy-night");
        reloaded.CustomVariables["--custom-primary-color"].Should().Be("#112233");
        (await service.SetAsync(user.Id, preference with { Theme = "unknown" })).IsFailure.Should().BeTrue();
        (await service.SetAsync(user.Id, preference with
            { CustomVariables = new Dictionary<string, string> { ["--unsafe"] = "red" } })).IsFailure.Should().BeTrue();
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
    public async Task Character_relationships_are_typed_editable_and_reject_exact_duplicates()
    {
        await using var db = CreateDb();
        var bookId = Guid.NewGuid();
        var hero = StoryStudioEntry.Create(bookId, "characters", "Hero", null, null, null, null, null);
        var rival = StoryStudioEntry.Create(bookId, "characters", "Rival", null, null, null, null, null);
        db.StoryStudioEntries.AddRange(hero, rival); await db.SaveChangesAsync();
        var service = new RelationshipService(db);
        var request = new CreateStoryRelationshipDto(hero.Id, rival.Id, null, "Mixed", "Rival", "They compete for the crown.", "Active");

        var created = await service.CreateRelationshipAsync(bookId, request);
        created.IsSuccess.Should().BeTrue();
        created.Value.Label.Should().Be("Rival");
        (await service.CreateRelationshipAsync(bookId, request)).IsFailure.Should().BeTrue();
        var updated = await service.UpdateRelationshipAsync(bookId, created.Value.Id, request with { Tone = "Negative", Status = "Broken" });
        updated.Value.Tone.Should().Be("Negative");
        updated.Value.Status.Should().Be("Broken");

        (await new StudioEntryService(db).DeleteEntryAsync(bookId, hero.Id)).IsSuccess.Should().BeTrue();
        (await service.ListRelationshipsAsync(bookId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Character_media_and_timeline_links_enforce_book_ownership_and_do_not_delete_sources()
    {
        await using var db = CreateDb();
        var bookId = Guid.NewGuid();
        var character = StoryStudioEntry.Create(bookId, "characters", "Hero", null, null, null, null, null);
        var media = StoryStudioEntry.Create(bookId, "gallery", "Portrait", null, null, null, null, null);
        var timeline = StoryStudioEntry.Create(bookId, "timeline", "Victory", null, null, null, null, null);
        var outsider = StoryStudioEntry.Create(Guid.NewGuid(), "gallery", "Private", null, null, null, null, null);
        db.StoryStudioEntries.AddRange(character, media, timeline, outsider); await db.SaveChangesAsync();
        var service = new CharacterConnectionsService(db);

        var mediaLink = await service.SaveMediaAsync(bookId, character.Id, null, new(media.Id, "Portrait", 0));
        mediaLink.IsSuccess.Should().BeTrue();
        (await service.SaveMediaAsync(bookId, character.Id, null, new(outsider.Id, "Portrait", 0))).IsFailure.Should().BeTrue();
        (await service.DeleteMediaAsync(bookId, character.Id, mediaLink.Value.Id)).IsSuccess.Should().BeTrue();
        (await db.StoryStudioEntries.AnyAsync(x => x.Id == media.Id)).Should().BeTrue();

        var eventLink = await service.SaveTimelineAsync(bookId, character.Id, null, new(timeline.Id, "Participant", "Wins the duel"));
        eventLink.IsSuccess.Should().BeTrue();
        var edited = await service.SaveTimelineAsync(bookId, character.Id, eventLink.Value.Id, new(timeline.Id, "Cause", "Starts the rebellion"));
        edited.Value.Role.Should().Be("Cause");
        (await service.DeleteTimelineAsync(bookId, character.Id, eventLink.Value.Id)).IsSuccess.Should().BeTrue();
        (await db.StoryStudioEntries.AnyAsync(x => x.Id == timeline.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Character_custom_attributes_validate_types_group_order_edit_and_delete()
    {
        await using var db = CreateDb();
        var bookId = Guid.NewGuid();
        var character = StoryStudioEntry.Create(bookId, "characters", "Hero", null, null, null, null, null);
        db.StoryStudioEntries.Add(character); await db.SaveChangesAsync();
        var service = new CharacterConnectionsService(db);

        (await service.SaveAttributeAsync(bookId, character.Id, null, new("Height", "Number", "not-a-number", "Appearance", 2, null))).IsFailure.Should().BeTrue();
        var rank = await service.SaveAttributeAsync(bookId, character.Id, null, new("Rank", "SingleSelect", "Mage", "Magic", 3, ["Mage", "Scholar"]));
        rank.IsSuccess.Should().BeTrue();
        (await service.SaveAttributeAsync(bookId, character.Id, null, new("Rank", "ShortText", "Other", "Magic", 4, null))).IsFailure.Should().BeTrue();
        var edited = await service.SaveAttributeAsync(bookId, character.Id, rank.Value.Id, new("Rank", "SingleSelect", "Scholar", "Magic", 1, ["Mage", "Scholar"]));
        edited.Value.Value.Should().Be("Scholar");
        edited.Value.DisplayOrder.Should().Be(1);
        (await service.DeleteAttributeAsync(bookId, character.Id, rank.Value.Id)).IsSuccess.Should().BeTrue();
        (await service.ListAttributesAsync(bookId, character.Id)).Value.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ShortText", "brief")]
    [InlineData("LongText", "A longer piece of character history")]
    [InlineData("Number", "42.5")]
    [InlineData("Boolean", "true")]
    [InlineData("Date", "2026-07-21")]
    public async Task Character_custom_attributes_support_each_scalar_type(string valueType, string value)
    {
        await using var db = CreateDb();
        var bookId = Guid.NewGuid();
        var character = StoryStudioEntry.Create(bookId, "characters", "Hero", null, null, null, null, null);
        db.StoryStudioEntries.Add(character); await db.SaveChangesAsync();

        var saved = await new CharacterConnectionsService(db).SaveAttributeAsync(bookId, character.Id, null,
            new SaveCharacterAttributeDto(valueType, valueType, value, "Facts", 0, null));

        saved.IsSuccess.Should().BeTrue();
        saved.Value.ValueType.Should().Be(valueType);
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

    private static string[] ForeignKeyProperties<TEntity>(LemonDbContext db) where TEntity : class =>
        db.Model.FindEntityType(typeof(TEntity))!.GetForeignKeys()
            .SelectMany(foreignKey => foreignKey.Properties).Select(property => property.Name).ToArray();
}
