using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Features.Books.AddBook;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.GetBook;
using BookStore.Application.Features.Books.GetBookCover;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Application.Features.Books.UpdateBook;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Features.Books;

[ApiController]
[Route("api/v1/books")]
public sealed class BooksController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var books = await mediator.Send(new GetBooksQuery(), cancellationToken);
        return Ok(books.Select(BookResponse.FromEntity).ToArray());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var book = await mediator.Send(new GetBookQuery(id), cancellationToken);
        return book is null ? NotFound() : Ok(BookResponse.FromEntity(book));
    }

    [HttpGet("{id:int}/cover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCover(int id, CancellationToken cancellationToken)
    {
        var cover = await mediator.Send(new GetBookCoverQuery(id), cancellationToken);
        return cover is null ? NotFound() : File(cover.Content, cover.ContentType);
    }

    [HttpPost]
    [ProducesResponseType<BookResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookResponse>> Import(
        ImportBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddBookCommand(
                request.Title,
                request.Author,
                request.Isbn,
                request.PublishedYear,
                request.CoverUrl,
                request.OpenLibraryKey,
                request.Description,
                request.Genre,
                request.Price,
                request.Stock),
            cancellationToken);

        if (result.Failure == AddBookFailure.InvalidPrice)
        {
            ModelState.AddModelError(nameof(request.Price), "Price cannot have more than two decimal places.");
            return ValidationProblem(ModelState);
        }

        if (result.Failure == AddBookFailure.Duplicate)
        {
            return DuplicateBookProblem();
        }

        var response = BookResponse.FromEntity(result.Book!);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<BookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookResponse>> Update(
        int id,
        UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateBookCommand(
                id,
                request.Genre,
                request.Price,
                request.Stock,
                request.Description),
            cancellationToken);

        if (result.Failure == UpdateBookFailure.InvalidPrice)
        {
            ModelState.AddModelError(nameof(request.Price), "Price cannot have more than two decimal places.");
            return ValidationProblem(ModelState);
        }

        return result.Failure == UpdateBookFailure.NotFound
            ? NotFound()
            : Ok(BookResponse.FromEntity(result.Book!));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteBookCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private ActionResult<BookResponse> DuplicateBookProblem() => Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "This Open Library book is already in the store.");
}
