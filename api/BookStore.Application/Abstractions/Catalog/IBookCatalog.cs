namespace BookStore.Application.Abstractions.Catalog;

public interface IBookCatalog
{
    Task<CatalogSearchPage> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<CatalogCover?> DownloadCoverAsync(
        string? coverUrl,
        CancellationToken cancellationToken);
}
