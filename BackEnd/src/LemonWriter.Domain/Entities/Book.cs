using LemonWriter.Domain.Common;
using LemonWriter.Domain.Events;

namespace LemonWriter.Domain.Entities;

public class Book : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public BookMetadata Metadata { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Book() { }

    public static Book Create(
        string title,
        string description,
        Guid authorId,
        BookMetadata metadata)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            AuthorId = authorId,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        book.RaiseDomainEvent(new BookCreatedEvent(book.Id, book.AuthorId, book.Title));
        return book;
    }

    public void Update(string title, string description, BookMetadata metadata)
    {
        Title = title;
        Description = description;
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }
}
