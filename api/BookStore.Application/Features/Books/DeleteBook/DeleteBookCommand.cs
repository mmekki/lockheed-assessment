using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;

namespace BookStore.Application.Features.Books.DeleteBook;

public sealed record DeleteBookCommand(int Id) : IRequest<bool>;

public sealed class DeleteBookHandler(IBookRepository books)
    : IRequestHandler<DeleteBookCommand, bool>
{
    public async Task<bool> Handle(
        DeleteBookCommand request,
        CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(request.Id, cancellationToken);
        if (book is null)
        {
            return false;
        }

        await books.DeleteAsync(book, cancellationToken);
        return true;
    }
}
