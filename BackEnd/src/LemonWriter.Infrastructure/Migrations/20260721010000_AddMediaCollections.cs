using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260721010000_AddMediaCollections")]
public sealed class AddMediaCollections : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("StoryMediaCollections", t => new
        {
            Id = t.Column<Guid>("uuid"), BookId = t.Column<Guid>("uuid"),
            Name = t.Column<string>("character varying(200)", maxLength: 200),
            CreatedAt = t.Column<DateTime>("timestamp with time zone")
        }, constraints: t => t.PrimaryKey("PK_StoryMediaCollections", x => x.Id));
        m.CreateIndex("IX_StoryMediaCollections_BookId_Name", "StoryMediaCollections", new[] { "BookId", "Name" });
        m.AddColumn<Guid>("CollectionId", "StoryStudioEntries", "uuid", nullable: true);
        m.CreateIndex("IX_StoryStudioEntries_CollectionId", "StoryStudioEntries", "CollectionId");
        m.AddForeignKey("FK_StoryStudioEntries_StoryMediaCollections_CollectionId", "StoryStudioEntries", "CollectionId",
            "StoryMediaCollections", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropForeignKey("FK_StoryStudioEntries_StoryMediaCollections_CollectionId", "StoryStudioEntries");
        m.DropIndex("IX_StoryStudioEntries_CollectionId", "StoryStudioEntries");
        m.DropColumn("CollectionId", "StoryStudioEntries");
        m.DropTable("StoryMediaCollections");
    }
}
