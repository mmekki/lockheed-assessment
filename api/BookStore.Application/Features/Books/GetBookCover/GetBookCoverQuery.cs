using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;

namespace BookStore.Application.Features.Books.GetBookCover;

public sealed record GetBookCoverQuery(int Id) : IRequest<BookCover?>;

public sealed record BookCover(byte[] Content, string ContentType);

public sealed class GetBookCoverHandler(IBookRepository books)
    : IRequestHandler<GetBookCoverQuery, BookCover?>
{
    public async Task<BookCover?> Handle(
        GetBookCoverQuery request,
        CancellationToken cancellationToken)
    {
        var book = await books.GetByIdAsync(request.Id, cancellationToken);

        return book?.CoverImage is null || book.CoverContentType is null
            ? null
            : new BookCover(book.CoverImage, book.CoverContentType);
    }
}
