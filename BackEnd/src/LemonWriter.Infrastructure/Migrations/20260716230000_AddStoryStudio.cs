using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260716230000_AddStoryStudio")]
public sealed class AddStoryStudio : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("StoryStudioEntries", t => new
        {
            Id = t.Column<Guid>("uuid"), BookId = t.Column<Guid>("uuid"), Type = t.Column<string>("character varying(32)", maxLength: 32),
            Name = t.Column<string>("character varying(300)", maxLength: 300), Summary = t.Column<string>("character varying(1000)", maxLength: 1000),
            Details = t.Column<string>("text"), Motivation = t.Column<string>("text", nullable: true), Plot = t.Column<string>("text", nullable: true),
            ImageData = t.Column<string>("text", nullable: true), CreatedAt = t.Column<DateTime>("timestamp with time zone"), UpdatedAt = t.Column<DateTime>("timestamp with time zone")
        }, constraints: t => t.PrimaryKey("PK_StoryStudioEntries", x => x.Id));
        m.CreateIndex("IX_StoryStudioEntries_BookId_Type", "StoryStudioEntries", new[] { "BookId", "Type" });
        m.CreateTable("StoryRelationships", t => new
        {
            Id = t.Column<Guid>("uuid"), BookId = t.Column<Guid>("uuid"), FromEntryId = t.Column<Guid>("uuid"), ToEntryId = t.Column<Guid>("uuid"),
            Label = t.Column<string>("character varying(300)", maxLength: 300), Tone = t.Column<string>("character varying(20)", maxLength: 20), CreatedAt = t.Column<DateTime>("timestamp with time zone")
        }, constraints: t => t.PrimaryKey("PK_StoryRelationships", x => x.Id));
        m.CreateIndex("IX_StoryRelationships_BookId", "StoryRelationships", "BookId");
    }
    protected override void Down(MigrationBuilder m) { m.DropTable("StoryRelationships"); m.DropTable("StoryStudioEntries"); }
}
