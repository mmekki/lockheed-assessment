using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;
using BookStore.Domain;

namespace BookStore.Application.Features.Books.UpdateBook;

public sealed record UpdateBookCommand(
    int Id,
    BookGenre Genre,
    decimal Price,
    int? Stock,
    string? Description) : IRequest<UpdateBookResult>;

public sealed record UpdateBookResult(Book? Book, UpdateBookFailure Failure = UpdateBookFailure.None);

public enum UpdateBookFailure
{
    None,
    InvalidPrice,
    NotFound
}

public sealed class UpdateBookHandler(IBookRepository books)
    : IRequestHandler<UpdateBookCommand, UpdateBookResult>
{
    public async Task<UpdateBookResult> Handle(
        UpdateBookCommand request,
        CancellationToken cancellationToken)
    {
        if (decimal.Round(request.Price, 2) != request.Price)
        {
            return new UpdateBookResult(null, UpdateBookFailure.InvalidPrice);
        }

        var book = await books.GetByIdAsync(request.Id, cancellationToken);
        if (book is null)
        {
            return new UpdateBookResult(null, UpdateBookFailure.NotFound);
        }

        book.Genre = request.Genre;
        book.Price = request.Price;
        book.Stock = request.Stock;
        book.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await books.UpdateAsync(book, cancellationToken);
        return new UpdateBookResult(book);
    }
}
