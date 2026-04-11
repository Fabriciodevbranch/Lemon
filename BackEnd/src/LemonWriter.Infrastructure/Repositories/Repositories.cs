using LemonWriter.Application.Common.Interfaces;
using LemonWriter.Domain.Entities;
using LemonWriter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LemonWriter.Infrastructure.Repositories;

public abstract class RepositoryBase<T> where T : class
{
    protected readonly LemonDbContext _context;
    protected RepositoryBase(LemonDbContext context) => _context = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Set<T>().ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _context.Set<T>().AddAsync(entity, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Set<T>().Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }
}

public class BookRepository : RepositoryBase<Book>, IBookRepository
{
    public BookRepository(LemonDbContext context) : base(context) { }

    public async Task<IEnumerable<Book>> GetByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default)
        => await _context.Books.Where(b => b.AuthorId == authorId).ToListAsync(cancellationToken);
}

public class ChapterRepository : RepositoryBase<Chapter>, IChapterRepository
{
    public ChapterRepository(LemonDbContext context) : base(context) { }

    public async Task<IEnumerable<Chapter>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
        => await _context.Chapters.Where(c => c.BookId == bookId).OrderBy(c => c.Order).ToListAsync(cancellationToken);
}

public class SnapshotRepository : RepositoryBase<Snapshot>, ISnapshotRepository
{
    public SnapshotRepository(LemonDbContext context) : base(context) { }

    public async Task<IEnumerable<Snapshot>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
        => await _context.Snapshots.Where(s => s.ChapterId == chapterId).OrderByDescending(s => s.CreatedAt).ToListAsync(cancellationToken);

    public async Task<Snapshot?> GetLatestByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
        => await _context.Snapshots.Where(s => s.ChapterId == chapterId).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(cancellationToken);
}

public class DraftRepository : RepositoryBase<Draft>, IDraftRepository
{
    public DraftRepository(LemonDbContext context) : base(context) { }

    public async Task<IEnumerable<Draft>> GetByChapterIdAsync(Guid chapterId, CancellationToken cancellationToken = default)
        => await _context.Drafts.Where(d => d.ChapterId == chapterId).ToListAsync(cancellationToken);
}

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(LemonDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<User?> GetByOAuthProviderAsync(string provider, string providerId, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.OAuthProvider == provider && u.OAuthProviderId == providerId, cancellationToken);
}
