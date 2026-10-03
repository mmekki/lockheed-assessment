using BookStore.Application.Abstractions.Catalog;
using BookStore.Application.Abstractions.Messaging;

namespace BookStore.Application.Features.Catalog.SearchCatalog;

public sealed record SearchCatalogQuery(string Query, int Page, int PageSize)
    : IRequest<CatalogSearchPage>;

public sealed class SearchCatalogHandler(IBookCatalog catalog)
    : IRequestHandler<SearchCatalogQuery, CatalogSearchPage>
{
    public Task<CatalogSearchPage> Handle(
        SearchCatalogQuery request,
        CancellationToken cancellationToken) =>
        catalog.SearchAsync(
            request.Query.Trim(),
            request.Page,
            request.PageSize,
            cancellationToken);
}
