using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BookStore.Application.Abstractions.Catalog;

namespace BookStore.Infrastructure.Catalog;

public sealed class OpenLibraryClient(HttpClient httpClient) : IBookCatalog
{
    private const string Fields = "key,title,author_name,isbn,first_publish_year,cover_i";
    private const int MaximumCoverBytes = 2 * 1024 * 1024;

    public async Task<CatalogSearchPage> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var path = $"search.json?q={Uri.EscapeDataString(query)}" +
            $"&fields={Fields}&page={page}&limit={pageSize}";

        OpenLibrarySearchResponse? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(
                path,
                cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CatalogTimeoutException(exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new CatalogUnavailableException(
                "Open Library returned an invalid or unsuccessful response.",
                exception);
        }

        if (response is null)
        {
            throw new CatalogUnavailableException("Open Library returned an empty response.");
        }

        var books = response.Documents
            .Where(document =>
                !string.IsNullOrWhiteSpace(document.Key) &&
                !string.IsNullOrWhiteSpace(document.Title))
            .Select(MapBook)
            .ToArray();

        return new CatalogSearchPage(response.Total, books);
    }

    public async Task<CatalogCover?> DownloadCoverAsync(
        string? coverUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            return await DownloadCoverCoreAsync(coverUrl, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CatalogTimeoutException(exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException)
        {
            throw new CatalogUnavailableException(
                "Open Library could not provide the selected cover.",
                exception);
        }
    }

    private async Task<CatalogCover?> DownloadCoverCoreAsync(
        string? coverUrl,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedCoverUrl(coverUrl, out var coverUri))
        {
            return null;
        }

        using var response = await httpClient.GetAsync(
            coverUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Open Library returned a non-image cover response.");
        }

        if (response.Content.Headers.ContentLength > MaximumCoverBytes)
        {
            throw new InvalidDataException("The Open Library cover exceeds the allowed size.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (destination.Length + bytesRead > MaximumCoverBytes)
            {
                throw new InvalidDataException("The Open Library cover exceeds the allowed size.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return new CatalogCover(destination.ToArray(), contentType);
    }

    private static bool IsAllowedCoverUrl(string? coverUrl, out Uri coverUri)
    {
        var isAllowed = Uri.TryCreate(coverUrl, UriKind.Absolute, out var parsedUri) &&
            parsedUri.Scheme == Uri.UriSchemeHttps &&
            parsedUri.Host.Equals("covers.openlibrary.org", StringComparison.OrdinalIgnoreCase) &&
            parsedUri.AbsolutePath.StartsWith("/b/id/", StringComparison.OrdinalIgnoreCase);

        coverUri = parsedUri ?? new Uri("https://covers.openlibrary.org/");
        return isAllowed;
    }

    private static CatalogBook MapBook(OpenLibrarySearchDocument document)
    {
        var author = document.AuthorNames.Count > 0
            ? string.Join(", ", document.AuthorNames)
            : "Unknown author";

        var coverUrl = document.CoverId is int coverId
            ? $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg?default=false"
            : null;

        return new CatalogBook(
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
