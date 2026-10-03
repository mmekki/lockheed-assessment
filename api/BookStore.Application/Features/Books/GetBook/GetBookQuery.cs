using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;
using BookStore.Domain;

namespace BookStore.Application.Features.Books.GetBook;

public sealed record GetBookQuery(int Id) : IRequest<Book?>;

public sealed class GetBookHandler(IBookRepository books)
    : IRequestHandler<GetBookQuery, Book?>
{
    public Task<Book?> Handle(GetBookQuery request, CancellationToken cancellationToken) =>
        books.GetByIdAsync(request.Id, cancellationToken);
}
