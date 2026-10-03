using BookStore.Application.Abstractions.Catalog;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Features.Books.AddBook;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.GetBook;
using BookStore.Application.Features.Books.GetBookCover;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Application.Features.Books.UpdateBook;
using BookStore.Domain;
using BookStore.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Tests.Features.Books;

public sealed class BookHandlersTests
{
    [Fact]
    public async Task ImportedBook_UsesOnlyRepositoryForSubsequentOperations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        var repository = new EfBookRepository(dbContext);
        var catalog = new StubBookCatalog
        {
            Cover = new CatalogCover([1, 2, 3], "image/jpeg")
        };

        var addResult = await new AddBookHandler(repository, catalog)
            .Handle(CreateAddCommand(), CancellationToken.None);

        Assert.Equal(AddBookFailure.None, addResult.Failure);
        Assert.NotNull(addResult.Book);
        Assert.Equal(1, catalog.DownloadCoverCalls);

        catalog.ThrowOnUse = true;

        var found = await new GetBookHandler(repository)
            .Handle(new GetBookQuery(addResult.Book.Id), CancellationToken.None);
        Assert.NotNull(found);

        var updateResult = await new UpdateBookHandler(repository)
            .Handle(
                new UpdateBookCommand(
                    addResult.Book.Id,
                    BookGenre.Children,
                    12.50m,
                    4,
                    "Stored locally"),
                CancellationToken.None);
        Assert.Equal(UpdateBookFailure.None, updateResult.Failure);
        Assert.Equal(12.50m, updateResult.Book?.Price);

        var cover = await new GetBookCoverHandler(repository)
            .Handle(new GetBookCoverQuery(addResult.Book.Id), CancellationToken.None);
        Assert.NotNull(cover);

        var deleted = await new DeleteBookHandler(repository)
            .Handle(new DeleteBookCommand(addResult.Book.Id), CancellationToken.None);
        Assert.True(deleted);
        Assert.Equal(1, catalog.DownloadCoverCalls);
    }

    [Fact]
    public async Task AddBook_ReturnsDuplicateForAnExistingOpenLibraryKey()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        var repository = new EfBookRepository(dbContext);
        var catalog = new StubBookCatalog();
        var handler = new AddBookHandler(repository, catalog);

        await handler.Handle(CreateAddCommand(), CancellationToken.None);
        var duplicate = await handler.Handle(CreateAddCommand(), CancellationToken.None);

        Assert.Equal(AddBookFailure.Duplicate, duplicate.Failure);
        Assert.Equal(1, catalog.DownloadCoverCalls);
    }

    [Fact]
    public async Task Mediator_ResolvesAndInvokesTheMatchingQueryHandler()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        var handler = new GetBooksHandler(new EfBookRepository(dbContext));
        var provider = new StubServiceProvider(
            typeof(IRequestHandler<GetBooksQuery, IReadOnlyList<Book>>),
            handler);
        var mediator = new Mediator(provider);

        var books = await mediator.Send(new GetBooksQuery(), CancellationToken.None);

        Assert.Empty(books);
    }

    private static BookStoreDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlite(connection)
            .Options;

        return new BookStoreDbContext(options);
    }

    private static AddBookCommand CreateAddCommand() => new(
        "Fantastic Mr. Fox",
        "Roald Dahl",
        "9780140328721",
        1970,
        "https://covers.openlibrary.org/b/id/8739161-M.jpg?default=false",
        "/works/OL45804W",
        "A clever fox takes on three farmers.",
        BookGenre.Children,
        10.99m,
        5);

    private sealed class StubBookCatalog : IBookCatalog
    {
        public CatalogCover? Cover { get; init; }

        public bool ThrowOnUse { get; set; }

        public int DownloadCoverCalls { get; private set; }

        public Task<CatalogSearchPage> SearchAsync(
            string query,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CatalogCover?> DownloadCoverAsync(
            string? coverUrl,
            CancellationToken cancellationToken)
        {
            if (ThrowOnUse)
            {
                throw new InvalidOperationException("The catalog must not be used after import.");
            }

            DownloadCoverCalls++;
            return Task.FromResult(Cover);
        }
    }

    private sealed class StubServiceProvider(Type serviceType, object service) : IServiceProvider
    {
        public object? GetService(Type requestedType) =>
            requestedType == serviceType ? service : null;
    }
}
