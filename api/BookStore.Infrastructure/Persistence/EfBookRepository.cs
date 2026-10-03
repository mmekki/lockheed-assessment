using BookStore.Application.Abstractions.Persistence;
using BookStore.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

public sealed class EfBookRepository(BookStoreDbContext dbContext) : IBookRepository
{
    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Books
            .AsNoTracking()
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .ToListAsync(cancellationToken);

    public Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Books.SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

    public Task<bool> ExistsByOpenLibraryKeyAsync(
        string openLibraryKey,
        CancellationToken cancellationToken) =>
        dbContext.Books.AnyAsync(
            book => book.OpenLibraryKey == openLibraryKey,
            cancellationToken);

    public async Task AddAsync(Book book, CancellationToken cancellationToken)
    {
        dbContext.Books.Add(book);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw new DuplicateBookException(exception);
        }
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken)
    {
        dbContext.Books.Update(book);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Book book, CancellationToken cancellationToken)
    {
        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
