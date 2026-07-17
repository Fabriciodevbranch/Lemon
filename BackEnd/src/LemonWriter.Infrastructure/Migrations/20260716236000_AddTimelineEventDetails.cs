using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260716236000_AddTimelineEventDetails")]
public sealed class AddTimelineEventDetails : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<string>("EventDate", "StoryStudioEntries", "character varying(100)", maxLength: 100, nullable: true);
        m.AddColumn<string>("Impact", "StoryStudioEntries", "text", nullable: true);
        m.AddColumn<string>("RelatedCharacterIds", "StoryStudioEntries", "text", nullable: true);
        m.AddColumn<string>("RelatedObjectIds", "StoryStudioEntries", "text", nullable: true);
        m.AddColumn<string>("RelatedPlaceIds", "StoryStudioEntries", "text", nullable: true);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropColumn("EventDate", "StoryStudioEntries"); m.DropColumn("Impact", "StoryStudioEntries");
        m.DropColumn("RelatedCharacterIds", "StoryStudioEntries"); m.DropColumn("RelatedObjectIds", "StoryStudioEntries");
        m.DropColumn("RelatedPlaceIds", "StoryStudioEntries");
    }
}
