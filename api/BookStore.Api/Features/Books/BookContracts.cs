using System.ComponentModel.DataAnnotations;
using BookStore.Domain;

namespace BookStore.Api.Features.Books;

public sealed class ImportBookRequest
{
    [Required, StringLength(500)]
    public required string Title { get; init; }

    [Required, StringLength(500)]
    public required string Author { get; init; }

    [StringLength(20)]
    public string? Isbn { get; init; }

    [Range(1, 9999)]
    public int? PublishedYear { get; init; }

    [Url, StringLength(1_000)]
    public string? CoverUrl { get; init; }

    [Required, RegularExpression(@"^/(works|books)/OL\d+[WM]$")]
    public required string OpenLibraryKey { get; init; }

    [StringLength(5_000)]
    public string? Description { get; init; }

    public BookGenre Genre { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int? Stock { get; init; }
}

public sealed class UpdateBookRequest
{
    public BookGenre Genre { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int? Stock { get; init; }

    [StringLength(5_000)]
    public string? Description { get; init; }
}

public sealed record BookResponse(
    int Id,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl,
    string OpenLibraryKey,
    string? Description,
    BookGenre Genre,
    decimal Price,
    int? Stock)
{
    public static BookResponse FromEntity(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Isbn,
        book.PublishedYear,
        book.CoverImage is null || book.CoverContentType is null
            ? book.CoverUrl
            : $"/api/v1/books/{book.Id}/cover",
        book.OpenLibraryKey,
        book.Description,
        book.Genre,
        book.Price,
        book.Stock);
}
