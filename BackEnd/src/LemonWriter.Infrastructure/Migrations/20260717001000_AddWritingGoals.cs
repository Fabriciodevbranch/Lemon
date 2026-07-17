using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260717001000_AddWritingGoals")]
public sealed class AddWritingGoals : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<int>("GoalTarget", "StoryStudioEntries", "integer", nullable: true);
        m.AddColumn<int>("GoalProgress", "StoryStudioEntries", "integer", nullable: false, defaultValue: 0);
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropColumn("GoalTarget", "StoryStudioEntries");
        m.DropColumn("GoalProgress", "StoryStudioEntries");
    }
}
