namespace BookStore.Api.Integrations.OpenLibrary;

public interface IOpenLibraryClient
{
    Task<OpenLibrarySearchPage> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
