using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;
using BookStore.Domain;

namespace BookStore.Application.Features.Books.GetBooks;

public sealed record GetBooksQuery : IRequest<IReadOnlyList<Book>>;

public sealed class GetBooksHandler(IBookRepository books)
    : IRequestHandler<GetBooksQuery, IReadOnlyList<Book>>
{
    public Task<IReadOnlyList<Book>> Handle(
        GetBooksQuery request,
        CancellationToken cancellationToken) =>
        books.GetAllAsync(cancellationToken);
}
