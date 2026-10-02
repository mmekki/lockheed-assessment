namespace BookStore.Api.Domain;

public sealed class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Author { get; set; }

    public string? Isbn { get; set; }

    public int? PublishedYear { get; set; }

    public string? CoverUrl { get; set; }

    public required string OpenLibraryKey { get; set; }

    public string? Description { get; set; }

    public BookGenre Genre { get; set; }

    public decimal Price { get; set; }

    public int? Stock { get; set; }
}
