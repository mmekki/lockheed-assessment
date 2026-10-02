using BookStore.Api.Data;
using BookStore.Api.Domain;
using BookStore.Api.Integrations.OpenLibrary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Features.Books;

[ApiController]
[Route("api/v1/books")]
public sealed class BooksController(
    BookStoreDbContext dbContext,
    IOpenLibraryClient openLibraryClient,
    ILogger<BooksController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var books = await dbContext.Books
            .AsNoTracking()
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .ToListAsync(cancellationToken);

        return Ok(books.Select(BookResponse.FromEntity).ToArray());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .AsNoTracking()
            .SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

        return book is null
            ? NotFound()
            : Ok(BookResponse.FromEntity(book));
    }

    [HttpGet("{id:int}/cover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCover(int id, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .AsNoTracking()
            .SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

        return book?.CoverImage is null || book.CoverContentType is null
            ? NotFound()
            : File(book.CoverImage, book.CoverContentType);
    }

    [HttpPost]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Import(
        ImportBookRequest request,
        CancellationToken cancellationToken)
    {
        if (HasMoreThanTwoDecimalPlaces(request.Price))
        {
            ModelState.AddModelError(nameof(request.Price), "Price cannot have more than two decimal places.");
            return ValidationProblem(ModelState);
        }

        var openLibraryKey = request.OpenLibraryKey.Trim();
        if (await dbContext.Books.AnyAsync(
                book => book.OpenLibraryKey == openLibraryKey,
                cancellationToken))
        {
            return DuplicateBookProblem();
        }

        var cover = await TryDownloadCoverAsync(request.CoverUrl, cancellationToken);
        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Isbn = NormalizeOptional(request.Isbn),
            PublishedYear = request.PublishedYear,
            OpenLibraryKey = openLibraryKey,
            Description = NormalizeOptional(request.Description),
            Genre = request.Genre,
            Price = request.Price,
            Stock = request.Stock,
            CoverImage = cover?.Content,
            CoverContentType = cover?.ContentType
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Books.Add(book);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            if (cover is not null)
            {
                book.CoverUrl = $"/api/v1/books/{book.Id}/cover";
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return DuplicateBookProblem();
        }

        var response = BookResponse.FromEntity(book);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> Update(
        int id,
        UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        if (HasMoreThanTwoDecimalPlaces(request.Price))
        {
            ModelState.AddModelError(nameof(request.Price), "Price cannot have more than two decimal places.");
            return ValidationProblem(ModelState);
        }

        var book = await dbContext.Books.SingleOrDefaultAsync(
            book => book.Id == id,
            cancellationToken);

        if (book is null)
        {
            return NotFound();
        }

        book.Genre = request.Genre;
        book.Price = request.Price;
        book.Stock = request.Stock;
        book.Description = NormalizeOptional(request.Description);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(BookResponse.FromEntity(book));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(
            book => book.Id == id,
            cancellationToken);

        if (book is null)
        {
            return NotFound();
        }

        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<OpenLibraryCover?> TryDownloadCoverAsync(
        string? coverUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            return await openLibraryClient.DownloadCoverAsync(coverUrl, cancellationToken);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidDataException or OperationCanceledException &&
            !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The selected cover could not be snapshotted during import.");
            return null;
        }
    }

    private ActionResult<BookResponse> DuplicateBookProblem() => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "This Open Library book is already in the store.");

    private static bool HasMoreThanTwoDecimalPlaces(decimal value) =>
        decimal.Round(value, 2) != value;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: 19 };
}
