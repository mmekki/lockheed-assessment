namespace BookStore.Api.Integrations.OpenLibrary;

public interface IOpenLibraryClient
{
    Task<OpenLibrarySearchPage> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OpenLibraryCover?> DownloadCoverAsync(
        string? coverUrl,
        CancellationToken cancellationToken);
}
