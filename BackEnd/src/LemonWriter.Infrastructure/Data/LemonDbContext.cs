using LemonWriter.Domain.Common;
using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Data;

public class LemonDbContext : DbContext, IUnitOfWork
{
    public LemonDbContext(DbContextOptions<LemonDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Snapshot> Snapshots => Set<Snapshot>();
    public DbSet<Draft> Drafts => Set<Draft>();
    public DbSet<User> Users => Set<User>();
    public DbSet<StoryStudioEntry> StoryStudioEntries => Set<StoryStudioEntry>();
    public DbSet<StoryRelationship> StoryRelationships => Set<StoryRelationship>();
    public DbSet<StoryMediaCollection> StoryMediaCollections => Set<StoryMediaCollection>();
    public DbSet<CharacterMediaReference> CharacterMediaReferences => Set<CharacterMediaReference>();
    public DbSet<CharacterTimelineReference> CharacterTimelineReferences => Set<CharacterTimelineReference>();
    public DbSet<CharacterCustomAttribute> CharacterCustomAttributes => Set<CharacterCustomAttribute>();
    public DbSet<CharacterAttributeOption> CharacterAttributeOptions => Set<CharacterAttributeOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LemonDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
