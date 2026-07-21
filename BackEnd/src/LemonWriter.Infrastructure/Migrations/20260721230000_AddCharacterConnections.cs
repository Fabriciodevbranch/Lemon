using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LemonWriter.Infrastructure.Migrations;
[DbContext(typeof(LemonDbContext)), Migration("20260721230000_AddCharacterConnections")]
public sealed class AddCharacterConnections : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<string>("RelationshipType", "StoryRelationships", "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Other");
        m.AddColumn<string>("Description", "StoryRelationships", "text", nullable: true);
        m.AddColumn<string>("Status", "StoryRelationships", "character varying(40)", maxLength: 40, nullable: true);
        m.CreateTable("CharacterMediaReferences", t => new { Id=t.Column<Guid>("uuid"),BookId=t.Column<Guid>("uuid"),CharacterId=t.Column<Guid>("uuid"),MediaId=t.Column<Guid>("uuid"),Role=t.Column<string>("character varying(40)",maxLength:40),DisplayOrder=t.Column<int>("integer") }, constraints:t=>t.PrimaryKey("PK_CharacterMediaReferences",x=>x.Id));
        m.CreateIndex("IX_CharacterMediaReferences_BookId_CharacterId_MediaId","CharacterMediaReferences",new[]{"BookId","CharacterId","MediaId"},unique:true);
        m.CreateTable("CharacterTimelineReferences", t => new { Id=t.Column<Guid>("uuid"),BookId=t.Column<Guid>("uuid"),CharacterId=t.Column<Guid>("uuid"),EventId=t.Column<Guid>("uuid"),Role=t.Column<string>("character varying(40)",maxLength:40),Note=t.Column<string>("text",nullable:true) }, constraints:t=>t.PrimaryKey("PK_CharacterTimelineReferences",x=>x.Id));
        m.CreateIndex("IX_CharacterTimelineReferences_BookId_CharacterId_EventId","CharacterTimelineReferences",new[]{"BookId","CharacterId","EventId"},unique:true);
        m.CreateTable("CharacterCustomAttributes", t => new { Id=t.Column<Guid>("uuid"),BookId=t.Column<Guid>("uuid"),CharacterId=t.Column<Guid>("uuid"),Label=t.Column<string>("character varying(160)",maxLength:160),ValueType=t.Column<string>("character varying(30)",maxLength:30),Value=t.Column<string>("text"),GroupName=t.Column<string>("character varying(100)",maxLength:100,nullable:true),DisplayOrder=t.Column<int>("integer") }, constraints:t=>t.PrimaryKey("PK_CharacterCustomAttributes",x=>x.Id));
        m.CreateIndex("IX_CharacterCustomAttributes_BookId_CharacterId","CharacterCustomAttributes",new[]{"BookId","CharacterId"});
        m.CreateTable("CharacterAttributeOptions", t => new { Id=t.Column<Guid>("uuid"),AttributeId=t.Column<Guid>("uuid"),Value=t.Column<string>("character varying(300)",maxLength:300),DisplayOrder=t.Column<int>("integer") }, constraints:t=>t.PrimaryKey("PK_CharacterAttributeOptions",x=>x.Id));
        m.CreateIndex("IX_CharacterAttributeOptions_AttributeId_DisplayOrder","CharacterAttributeOptions",new[]{"AttributeId","DisplayOrder"});
    }
    protected override void Down(MigrationBuilder m)
    { m.DropTable("CharacterAttributeOptions");m.DropTable("CharacterCustomAttributes");m.DropTable("CharacterMediaReferences");m.DropTable("CharacterTimelineReferences");m.DropColumn("RelationshipType","StoryRelationships");m.DropColumn("Description","StoryRelationships");m.DropColumn("Status","StoryRelationships"); }
}
