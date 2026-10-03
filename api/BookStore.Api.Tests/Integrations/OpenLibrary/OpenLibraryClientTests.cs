using System.Net;
using System.Text;
using BookStore.Infrastructure.Catalog;

namespace BookStore.Api.Tests.Integrations.OpenLibrary;

public sealed class OpenLibraryClientTests
{
    [Fact]
    public async Task SearchAsync_MapsTheRequiredBookFields()
    {
        const string response = """
            {
              "numFound": 1,
              "docs": [
                {
                  "key": "/works/OL45804W",
                  "title": "Fantastic Mr. Fox",
                  "author_name": ["Roald Dahl", "Second Author"],
                  "isbn": ["9780140328721", "0140328726"],
                  "first_publish_year": 1970,
                  "cover_i": 8739161
                }
              ]
            }
            """;
        var handler = new StubHttpMessageHandler(response);
        var client = CreateClient(handler);

        var result = await client.SearchAsync("fantastic fox", 2, 5, CancellationToken.None);

        var book = Assert.Single(result.Books);
        Assert.Equal(1, result.Total);
        Assert.Equal("/works/OL45804W", book.OpenLibraryKey);
        Assert.Equal("Fantastic Mr. Fox", book.Title);
        Assert.Equal("Roald Dahl, Second Author", book.Author);
        Assert.Equal("9780140328721", book.Isbn);
        Assert.Equal(1970, book.PublishedYear);
        Assert.Equal(
            "https://covers.openlibrary.org/b/id/8739161-M.jpg?default=false",
            book.CoverUrl);
        Assert.Contains("q=fantastic%20fox", handler.RequestUri?.Query);
        Assert.Contains("page=2", handler.RequestUri?.Query);
        Assert.Contains("limit=5", handler.RequestUri?.Query);
    }

    [Fact]
    public async Task SearchAsync_HandlesLegacyTotalsAndMissingOptionalFields()
    {
        const string response = """
            {
              "num_found": 12,
              "docs": [
                {
                  "key": "/works/OL1W",
                  "title": "Untitled Author"
                }
              ]
            }
            """;
        var client = CreateClient(new StubHttpMessageHandler(response));

        var result = await client.SearchAsync("query", 1, 10, CancellationToken.None);

        var book = Assert.Single(result.Books);
        Assert.Equal(12, result.Total);
        Assert.Equal("Unknown author", book.Author);
        Assert.Null(book.Isbn);
        Assert.Null(book.PublishedYear);
        Assert.Null(book.CoverUrl);
    }

    [Fact]
    public async Task DownloadCoverAsync_DownloadsOnlyTrustedImageUrls()
    {
        var expectedContent = new byte[] { 1, 2, 3, 4 };
        var handler = new StubHttpMessageHandler(expectedContent, "image/jpeg");
        var client = CreateClient(handler);

        var cover = await client.DownloadCoverAsync(
            "https://covers.openlibrary.org/b/id/8739161-M.jpg?default=false",
            CancellationToken.None);

        Assert.NotNull(cover);
        Assert.Equal(expectedContent, cover.Content);
        Assert.Equal("image/jpeg", cover.ContentType);
        Assert.Equal(1, handler.SendCount);

        var untrustedCover = await client.DownloadCoverAsync(
            "https://example.com/not-a-cover.jpg",
            CancellationToken.None);

        Assert.Null(untrustedCover);
        Assert.Equal(1, handler.SendCount);
    }

    private static OpenLibraryClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://openlibrary.org/")
        };

        return new OpenLibraryClient(httpClient);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] content;
        private readonly string contentType;

        public StubHttpMessageHandler(string content)
            : this(Encoding.UTF8.GetBytes(content), "application/json")
        {
        }

        public StubHttpMessageHandler(byte[] content, string contentType)
        {
            this.content = content;
            this.contentType = contentType;
        }

        public Uri? RequestUri { get; private set; }

        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            SendCount++;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
                {
                    Headers =
                    {
                        ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType)
                    }
                }
            });
        }
    }
}
