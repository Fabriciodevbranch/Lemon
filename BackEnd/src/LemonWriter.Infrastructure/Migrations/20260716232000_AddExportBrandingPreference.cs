using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;

[DbContext(typeof(LemonDbContext))]
[Migration("20260716232000_AddExportBrandingPreference")]
public sealed class AddExportBrandingPreference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(
            name: "IncludeExportBranding",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "IncludeExportBranding", table: "Users");
}
