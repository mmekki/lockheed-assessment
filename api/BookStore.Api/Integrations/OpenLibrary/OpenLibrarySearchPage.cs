namespace BookStore.Api.Integrations.OpenLibrary;

public sealed record OpenLibrarySearchPage(
    int Total,
    IReadOnlyList<OpenLibraryBook> Books);

public sealed record OpenLibraryBook(
    string OpenLibraryKey,
    string Title,
    string Author,
    string? Isbn,
    int? PublishedYear,
    string? CoverUrl);
