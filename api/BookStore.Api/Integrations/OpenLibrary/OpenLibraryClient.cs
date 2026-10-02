using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BookStore.Api.Integrations.OpenLibrary;

public sealed class OpenLibraryClient(HttpClient httpClient) : IOpenLibraryClient
{
    private const string Fields = "key,title,author_name,isbn,first_publish_year,cover_i";

    public async Task<OpenLibrarySearchPage> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var path = $"search.json?q={Uri.EscapeDataString(query)}" +
            $"&fields={Fields}&page={page}&limit={pageSize}";

        var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(
            path,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidDataException("Open Library returned an empty response.");
        }

        var books = response.Documents
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.Key) &&
                !string.IsNullOrWhiteSpace(document.Title))
            .Select(MapBook)
            .ToArray();

        return new OpenLibrarySearchPage(response.Total, books);
    }

    private static OpenLibraryBook MapBook(OpenLibrarySearchDocument document)
    {
        var author = document.AuthorNames.Count > 0
            ? string.Join(", ", document.AuthorNames)
            : "Unknown author";

        var coverUrl = document.CoverId is int coverId
            ? $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg?default=false"
            : null;

        return new OpenLibraryBook(
            document.Key!,
            document.Title!,
            author,
            document.Isbns.FirstOrDefault(),
            document.FirstPublishedYear,
            coverUrl);
    }

    private sealed class OpenLibrarySearchResponse
    {
        [JsonPropertyName("numFound")]
        public int? NumFound { get; init; }

        [JsonPropertyName("num_found")]
        public int? LegacyNumFound { get; init; }

        [JsonIgnore]
        public int Total => NumFound ?? LegacyNumFound ?? 0;

        [JsonPropertyName("docs")]
        public IReadOnlyList<OpenLibrarySearchDocument> Documents { get; init; } = [];
    }

    private sealed class OpenLibrarySearchDocument
    {
        [JsonPropertyName("key")]
        public string? Key { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("author_name")]
        public IReadOnlyList<string> AuthorNames { get; init; } = [];

        [JsonPropertyName("isbn")]
        public IReadOnlyList<string> Isbns { get; init; } = [];

        [JsonPropertyName("first_publish_year")]
        public int? FirstPublishedYear { get; init; }

        [JsonPropertyName("cover_i")]
        public int? CoverId { get; init; }
    }
}
