using BookStore.Api.Data;
using BookStore.Api.Domain;
using BookStore.Api.Features.Books;
using BookStore.Api.Integrations.OpenLibrary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Api.Tests.Features.Books;

public sealed class BooksControllerTests
{
    [Fact]
    public async Task ImportedBook_UsesOnlySQLiteForSubsequentOperations()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        var openLibrary = new StubOpenLibraryClient
        {
            Cover = new OpenLibraryCover([1, 2, 3], "image/jpeg")
        };
        var controller = CreateController(dbContext, openLibrary);

        var importResult = await controller.Import(CreateImportRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(importResult.Result);
        var importedBook = Assert.IsType<BookResponse>(created.Value);
        Assert.Equal("/api/v1/books/1/cover", importedBook.CoverUrl);
        Assert.Equal(1, openLibrary.DownloadCoverCalls);

        openLibrary.ThrowOnUse = true;

        var getResult = await controller.GetById(importedBook.Id, CancellationToken.None);
        var found = Assert.IsType<OkObjectResult>(getResult.Result);
        Assert.IsType<BookResponse>(found.Value);

        var updateResult = await controller.Update(
            importedBook.Id,
            new UpdateBookRequest
            {
                Genre = BookGenre.Children,
                Price = 12.50m,
                Stock = 4,
                Description = "Stored locally"
            },
            CancellationToken.None);
        var updated = Assert.IsType<OkObjectResult>(updateResult.Result);
        var updatedBook = Assert.IsType<BookResponse>(updated.Value);
        Assert.Equal(12.50m, updatedBook.Price);

        var coverResult = await controller.GetCover(importedBook.Id, CancellationToken.None);
        Assert.IsType<FileContentResult>(coverResult);

        var deleteResult = await controller.Delete(importedBook.Id, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleteResult);
        Assert.Equal(1, openLibrary.DownloadCoverCalls);
    }

    [Fact]
    public async Task Import_ReturnsConflictForAnExistingOpenLibraryKey()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = CreateDbContext(connection);
        await dbContext.Database.EnsureCreatedAsync();
        var openLibrary = new StubOpenLibraryClient();
        var controller = CreateController(dbContext, openLibrary);

        await controller.Import(CreateImportRequest(), CancellationToken.None);
        var duplicateResult = await controller.Import(CreateImportRequest(), CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(duplicateResult.Result);
        Assert.Equal(409, conflict.StatusCode);
        Assert.Equal(1, openLibrary.DownloadCoverCalls);
    }

    private static BookStoreDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlite(connection)
            .Options;

        return new BookStoreDbContext(options);
    }

    private static BooksController CreateController(
        BookStoreDbContext dbContext,
        IOpenLibraryClient openLibraryClient) =>
        new(
            dbContext,
            openLibraryClient,
            NullLogger<BooksController>.Instance);

    private static ImportBookRequest CreateImportRequest() => new()
    {
        Title = "Fantastic Mr. Fox",
        Author = "Roald Dahl",
        Isbn = "9780140328721",
        PublishedYear = 1970,
        CoverUrl = "https://covers.openlibrary.org/b/id/8739161-M.jpg?default=false",
        OpenLibraryKey = "/works/OL45804W",
        Description = "A clever fox takes on three farmers.",
        Genre = BookGenre.Children,
        Price = 10.99m,
        Stock = 5
    };

    private sealed class StubOpenLibraryClient : IOpenLibraryClient
    {
        public OpenLibraryCover? Cover { get; init; }

        public bool ThrowOnUse { get; set; }

        public int DownloadCoverCalls { get; private set; }

        public Task<OpenLibrarySearchPage> SearchAsync(
            string query,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<OpenLibraryCover?> DownloadCoverAsync(
            string? coverUrl,
            CancellationToken cancellationToken)
        {
            if (ThrowOnUse)
            {
                throw new InvalidOperationException("Open Library must not be used after import.");
            }

            DownloadCoverCalls++;
            return Task.FromResult(Cover);
        }
    }
}
