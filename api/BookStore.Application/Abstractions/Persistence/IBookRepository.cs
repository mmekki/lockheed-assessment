using BookStore.Domain;

namespace BookStore.Application.Abstractions.Persistence;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken cancellationToken);

    Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsByOpenLibraryKeyAsync(
        string openLibraryKey,
        CancellationToken cancellationToken);

    Task AddAsync(Book book, CancellationToken cancellationToken);

    Task UpdateAsync(Book book, CancellationToken cancellationToken);

    Task DeleteAsync(Book book, CancellationToken cancellationToken);
}
