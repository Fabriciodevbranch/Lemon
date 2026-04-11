using LemonWriter.Domain.Common;
using LemonWriter.Domain.Events;

namespace LemonWriter.Domain.Entities;

public class Chapter : AggregateRoot<Guid>
{
    public Guid BookId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public string CurrentContent { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Chapter() { }

    public static Chapter Create(Guid bookId, string title, int order)
    {
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Title = title,
            Order = order,
            CurrentContent = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        chapter.RaiseDomainEvent(new ChapterCreatedEvent(chapter.Id, chapter.BookId, chapter.Title));
        return chapter;
    }

    public void Update(string title, string content, int order)
    {
        Title = title;
        CurrentContent = content;
        Order = order;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RestoreContent(string content)
    {
        CurrentContent = content;
        UpdatedAt = DateTime.UtcNow;
    }
}
