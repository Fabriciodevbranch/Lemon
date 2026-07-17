using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260716235000_AddStudioSortOrder")]
public sealed class AddStudioSortOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<int>("SortOrder", "StoryStudioEntries", "integer", nullable: false, defaultValue: 0);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("SortOrder", "StoryStudioEntries");
}
