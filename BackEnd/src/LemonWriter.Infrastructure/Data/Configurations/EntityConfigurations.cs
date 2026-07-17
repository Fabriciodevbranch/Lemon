using LemonWriter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LemonWriter.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).IsRequired().HasMaxLength(500);
        builder.Property(b => b.Description).HasMaxLength(5000);
        builder.Property(b => b.AuthorId).IsRequired();

        builder.OwnsOne(b => b.Metadata, meta =>
        {
            meta.Property(m => m.AuthorName).HasColumnName("AuthorName").HasMaxLength(200);
            meta.Property(m => m.ISBN).HasColumnName("ISBN").HasMaxLength(20);
            meta.Property(m => m.INBR).HasColumnName("INBR").HasMaxLength(20);
            meta.Property(m => m.CoverImageUrl).HasColumnName("CoverImageUrl").HasColumnType("text");
            meta.Property(m => m.Description).HasColumnName("MetadataDescription").HasMaxLength(5000);
            meta.Property(m => m.IsSeries).HasColumnName("IsSeries");
            meta.Property(m => m.SeriesVolume).HasColumnName("SeriesVolume");
            meta.Property(m => m.SeriesName).HasColumnName("SeriesName").HasMaxLength(500);
        });

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();
    }
}

public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(500);
        builder.Property(c => c.BookId).IsRequired();
        builder.Property(c => c.Order).IsRequired();
        builder.Property(c => c.CurrentContent);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}

public class SnapshotConfiguration : IEntityTypeConfiguration<Snapshot>
{
    public void Configure(EntityTypeBuilder<Snapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ChapterId).IsRequired();
        builder.Property(s => s.Content).IsRequired();
        builder.Property(s => s.SnapshotMessage).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.AuthorId).IsRequired();
        builder.Property(s => s.ParentSnapshotId);
        builder.Property(s => s.CreatedAt).IsRequired();
    }
}

public class DraftConfiguration : IEntityTypeConfiguration<Draft>
{
    public void Configure(EntityTypeBuilder<Draft> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.ChapterId).IsRequired();
        builder.Property(d => d.Title).IsRequired().HasMaxLength(500);
        builder.Property(d => d.Content);
        builder.Property(d => d.IsPublished).IsRequired();
        builder.Property(d => d.PublishedAt);
        builder.Property(d => d.PublishedSnapshotId);
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.OAuthProvider).HasMaxLength(100);
        builder.Property(u => u.OAuthProviderId).HasMaxLength(256);
        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.IncludeExportBranding).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.StoryMetricsEnabled).IsRequired().HasDefaultValue(false);
    }
}

public class StoryStudioEntryConfiguration : IEntityTypeConfiguration<StoryStudioEntry>
{
    public void Configure(EntityTypeBuilder<StoryStudioEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Summary).HasMaxLength(1000);
        builder.Property(x => x.Details).HasColumnType("text");
        builder.Property(x => x.Motivation).HasColumnType("text");
        builder.Property(x => x.Plot).HasColumnType("text");
        builder.Property(x => x.ImageData).HasColumnType("text");
        builder.Property(x => x.SortOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.EventDate).HasMaxLength(100);
        builder.Property(x => x.Impact).HasColumnType("text");
        builder.Property(x => x.RelatedCharacterIds).HasColumnType("text");
        builder.Property(x => x.RelatedObjectIds).HasColumnType("text");
        builder.Property(x => x.RelatedPlaceIds).HasColumnType("text");
        builder.Property(x => x.GoalTarget);
        builder.Property(x => x.GoalProgress).IsRequired().HasDefaultValue(0);
        builder.HasIndex(x => new { x.BookId, x.Type });
    }
}

public class StoryRelationshipConfiguration : IEntityTypeConfiguration<StoryRelationship>
{
    public void Configure(EntityTypeBuilder<StoryRelationship> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Tone).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.BookId);
    }
}
