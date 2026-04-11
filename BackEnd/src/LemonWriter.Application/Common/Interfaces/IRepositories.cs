using LemonWriter.Domain.Entities;
using LemonWriter.Domain.Interfaces;

namespace LemonWriter.Application.Common.Interfaces;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default);
}

public interface IChapterRepository : IRepository<Chapter>
{
    Task<IEnumerable<Chapter>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
}

public interface ISnapshotRepository : IRepository<Snapshot>
{
    Task<IEnumerable<Snapshot>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default);
    Task<Snapshot?> GetLatestByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default);
}

public interface IDraftRepository : IRepository<Draft>
{
    Task<IEnumerable<Draft>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByOAuthProviderAsync(string provider, string providerId, CancellationToken cancellationToken = default);
}
