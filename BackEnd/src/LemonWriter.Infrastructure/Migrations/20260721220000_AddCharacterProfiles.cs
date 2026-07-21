using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260721220000_AddCharacterProfiles")]
public sealed class AddCharacterProfiles : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        Add(m, "StoryRole", "character varying(40)", 40); Add(m, "CharacterStatus", "character varying(40)", 40);
        Add(m, "Age", "character varying(80)", 80); Add(m, "Pronouns", "character varying(100)", 100);
        foreach (var name in new[] { "Aliases", "ExternalGoal", "InternalNeed", "Fear", "Secret", "InternalConflict",
            "ExternalConflict", "NarrativeFunction", "ArcSummary", "StartingState", "TurningPoint", "EndingState", "Notes" })
            m.AddColumn<string>(name, "StoryStudioEntries", "text", nullable: true);
        m.AddColumn<Guid>("PortraitMediaId", "StoryStudioEntries", "uuid", nullable: true);
    }

    protected override void Down(MigrationBuilder m)
    {
        foreach (var name in new[] { "StoryRole", "CharacterStatus", "Age", "Pronouns", "Aliases", "PortraitMediaId",
            "ExternalGoal", "InternalNeed", "Fear", "Secret", "InternalConflict", "ExternalConflict", "NarrativeFunction",
            "ArcSummary", "StartingState", "TurningPoint", "EndingState", "Notes" }) m.DropColumn(name, "StoryStudioEntries");
    }

    private static void Add(MigrationBuilder m, string name, string type, int maxLength) =>
        m.AddColumn<string>(name, "StoryStudioEntries", type, maxLength: maxLength, nullable: true);
}
