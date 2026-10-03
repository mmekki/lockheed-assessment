namespace BookStore.Application.Abstractions.Catalog;

public sealed record CatalogSearchPage(
    int Total,
    IReadOnlyList<CatalogBook> Books);

public sealed record CatalogBook(
    string OpenLibraryKey,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl);

public sealed record CatalogCover(byte[] Content, string ContentType);
