using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext)), Migration("20260721233000_AddCharacterConnectionForeignKeys")]
public sealed class AddCharacterConnectionForeignKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep this corrective migration deployable even if links were inserted manually before constraints existed.
        migrationBuilder.Sql("""
            DELETE FROM "CharacterMediaReferences" link
            WHERE NOT EXISTS (SELECT 1 FROM "Books" b WHERE b."Id" = link."BookId")
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = link."CharacterId" AND e."BookId" = link."BookId" AND e."Type" = 'characters')
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = link."MediaId" AND e."BookId" = link."BookId" AND e."Type" = 'gallery');
            DELETE FROM "CharacterTimelineReferences" link
            WHERE NOT EXISTS (SELECT 1 FROM "Books" b WHERE b."Id" = link."BookId")
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = link."CharacterId" AND e."BookId" = link."BookId" AND e."Type" = 'characters')
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = link."EventId" AND e."BookId" = link."BookId" AND e."Type" = 'timeline');
            DELETE FROM "CharacterCustomAttributes" attribute
            WHERE NOT EXISTS (SELECT 1 FROM "Books" b WHERE b."Id" = attribute."BookId")
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = attribute."CharacterId" AND e."BookId" = attribute."BookId" AND e."Type" = 'characters');
            DELETE FROM "CharacterAttributeOptions" option
            WHERE NOT EXISTS (SELECT 1 FROM "CharacterCustomAttributes" attribute WHERE attribute."Id" = option."AttributeId");
            DELETE FROM "StoryRelationships" relationship
            WHERE NOT EXISTS (SELECT 1 FROM "Books" b WHERE b."Id" = relationship."BookId")
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = relationship."FromEntryId" AND e."BookId" = relationship."BookId" AND e."Type" = 'characters')
               OR NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" e WHERE e."Id" = relationship."ToEntryId" AND e."BookId" = relationship."BookId" AND e."Type" = 'characters');
            UPDATE "StoryStudioEntries" character SET "PortraitMediaId" = NULL
            WHERE "PortraitMediaId" IS NOT NULL
              AND NOT EXISTS (SELECT 1 FROM "StoryStudioEntries" media WHERE media."Id" = character."PortraitMediaId" AND media."BookId" = character."BookId" AND media."Type" = 'gallery');
            """);

        CreateIndexes(migrationBuilder);
        AddForeignKeys(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var name in ForeignKeyNames) migrationBuilder.DropForeignKey(name, TableFor(name));
        migrationBuilder.DropIndex("IX_StoryStudioEntries_PortraitMediaId", "StoryStudioEntries");
        migrationBuilder.DropIndex("IX_StoryRelationships_FromEntryId", "StoryRelationships");
        migrationBuilder.DropIndex("IX_StoryRelationships_ToEntryId", "StoryRelationships");
        migrationBuilder.DropIndex("IX_CharacterMediaReferences_CharacterId", "CharacterMediaReferences");
        migrationBuilder.DropIndex("IX_CharacterMediaReferences_MediaId", "CharacterMediaReferences");
        migrationBuilder.DropIndex("IX_CharacterTimelineReferences_CharacterId", "CharacterTimelineReferences");
        migrationBuilder.DropIndex("IX_CharacterTimelineReferences_EventId", "CharacterTimelineReferences");
        migrationBuilder.DropIndex("IX_CharacterCustomAttributes_CharacterId", "CharacterCustomAttributes");
    }

    private static void CreateIndexes(MigrationBuilder m)
    {
        m.CreateIndex("IX_StoryStudioEntries_PortraitMediaId", "StoryStudioEntries", "PortraitMediaId");
        m.CreateIndex("IX_StoryRelationships_FromEntryId", "StoryRelationships", "FromEntryId");
        m.CreateIndex("IX_StoryRelationships_ToEntryId", "StoryRelationships", "ToEntryId");
        m.CreateIndex("IX_CharacterMediaReferences_CharacterId", "CharacterMediaReferences", "CharacterId");
        m.CreateIndex("IX_CharacterMediaReferences_MediaId", "CharacterMediaReferences", "MediaId");
        m.CreateIndex("IX_CharacterTimelineReferences_CharacterId", "CharacterTimelineReferences", "CharacterId");
        m.CreateIndex("IX_CharacterTimelineReferences_EventId", "CharacterTimelineReferences", "EventId");
        m.CreateIndex("IX_CharacterCustomAttributes_CharacterId", "CharacterCustomAttributes", "CharacterId");
    }

    private static void AddForeignKeys(MigrationBuilder m)
    {
        Add(m, "FK_StoryStudioEntries_StoryStudioEntries_PortraitMediaId", "StoryStudioEntries", "PortraitMediaId", "StoryStudioEntries", ReferentialAction.SetNull);
        Add(m, "FK_StoryRelationships_Books_BookId", "StoryRelationships", "BookId", "Books");
        Add(m, "FK_StoryRelationships_StoryStudioEntries_FromEntryId", "StoryRelationships", "FromEntryId", "StoryStudioEntries");
        Add(m, "FK_StoryRelationships_StoryStudioEntries_ToEntryId", "StoryRelationships", "ToEntryId", "StoryStudioEntries");
        Add(m, "FK_CharacterMediaReferences_Books_BookId", "CharacterMediaReferences", "BookId", "Books");
        Add(m, "FK_CharacterMediaReferences_StoryStudioEntries_CharacterId", "CharacterMediaReferences", "CharacterId", "StoryStudioEntries");
        Add(m, "FK_CharacterMediaReferences_StoryStudioEntries_MediaId", "CharacterMediaReferences", "MediaId", "StoryStudioEntries");
        Add(m, "FK_CharacterTimelineReferences_Books_BookId", "CharacterTimelineReferences", "BookId", "Books");
        Add(m, "FK_CharacterTimelineReferences_StoryStudioEntries_CharacterId", "CharacterTimelineReferences", "CharacterId", "StoryStudioEntries");
        Add(m, "FK_CharacterTimelineReferences_StoryStudioEntries_EventId", "CharacterTimelineReferences", "EventId", "StoryStudioEntries");
        Add(m, "FK_CharacterCustomAttributes_Books_BookId", "CharacterCustomAttributes", "BookId", "Books");
        Add(m, "FK_CharacterCustomAttributes_StoryStudioEntries_CharacterId", "CharacterCustomAttributes", "CharacterId", "StoryStudioEntries");
        Add(m, "FK_CharacterAttributeOptions_CharacterCustomAttributes_AttributeId", "CharacterAttributeOptions", "AttributeId", "CharacterCustomAttributes");
    }

    private static void Add(MigrationBuilder m, string name, string table, string column, string principalTable,
        ReferentialAction onDelete = ReferentialAction.Cascade) =>
        m.AddForeignKey(name: name, table: table, column: column, principalTable: principalTable,
            principalColumn: "Id", onDelete: onDelete);

    private static readonly string[] ForeignKeyNames =
    [
        "FK_StoryStudioEntries_StoryStudioEntries_PortraitMediaId",
        "FK_StoryRelationships_Books_BookId",
        "FK_StoryRelationships_StoryStudioEntries_FromEntryId",
        "FK_StoryRelationships_StoryStudioEntries_ToEntryId",
        "FK_CharacterMediaReferences_Books_BookId",
        "FK_CharacterMediaReferences_StoryStudioEntries_CharacterId",
        "FK_CharacterMediaReferences_StoryStudioEntries_MediaId",
        "FK_CharacterTimelineReferences_Books_BookId",
        "FK_CharacterTimelineReferences_StoryStudioEntries_CharacterId",
        "FK_CharacterTimelineReferences_StoryStudioEntries_EventId",
        "FK_CharacterCustomAttributes_Books_BookId",
        "FK_CharacterCustomAttributes_StoryStudioEntries_CharacterId",
        "FK_CharacterAttributeOptions_CharacterCustomAttributes_AttributeId"
    ];

    private static string TableFor(string foreignKey) => foreignKey.StartsWith("FK_StoryStudioEntries_") ? "StoryStudioEntries"
        : foreignKey.StartsWith("FK_StoryRelationships_") ? "StoryRelationships"
        : foreignKey.StartsWith("FK_CharacterMediaReferences_") ? "CharacterMediaReferences"
        : foreignKey.StartsWith("FK_CharacterTimelineReferences_") ? "CharacterTimelineReferences"
        : foreignKey.StartsWith("FK_CharacterCustomAttributes_") ? "CharacterCustomAttributes"
        : "CharacterAttributeOptions";
}
