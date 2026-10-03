using BookStore.Application.Abstractions.Catalog;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;
using BookStore.Domain;

namespace BookStore.Application.Features.Books.AddBook;

public sealed record AddBookCommand(
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    string OpenLibraryKey,
    string? Description,
    BookGenre Genre,
    decimal Price,
    int? Stock) : IRequest<AddBookResult>;

public sealed record AddBookResult(Book? Book, AddBookFailure Failure = AddBookFailure.None);

public enum AddBookFailure
{
    None,
    InvalidPrice,
    Duplicate
}

public sealed class AddBookHandler(IBookRepository books, IBookCatalog catalog)
    : IRequestHandler<AddBookCommand, AddBookResult>
{
    public async Task<AddBookResult> Handle(
        AddBookCommand request,
        CancellationToken cancellationToken)
    {
        if (decimal.Round(request.Price, 2) != request.Price)
        {
            return new AddBookResult(null, AddBookFailure.InvalidPrice);
        }

        var openLibraryKey = request.OpenLibraryKey.Trim();
        if (await books.ExistsByOpenLibraryKeyAsync(openLibraryKey, cancellationToken))
        {
            return new AddBookResult(null, AddBookFailure.Duplicate);
        }

        CatalogCover? cover = null;
        try
        {
            cover = await catalog.DownloadCoverAsync(request.CoverUrl, cancellationToken);
        }
        catch (CatalogUnavailableException)
        {
            // Cover snapshots are best-effort and must not prevent importing the book.
        }

        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Isbn = NormalizeOptional(request.Isbn),
            PublishedYear = request.PublishedYear,
            CoverUrl = request.CoverUrl,
            OpenLibraryKey = openLibraryKey,
            Description = NormalizeOptional(request.Description),
            Genre = request.Genre,
            Price = request.Price,
            Stock = request.Stock,
            CoverImage = cover?.Content,
            CoverContentType = cover?.ContentType
        };

        try
        {
            await books.AddAsync(book, cancellationToken);
        }
        catch (DuplicateBookException)
        {
            return new AddBookResult(null, AddBookFailure.Duplicate);
        }

        return new AddBookResult(book);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
