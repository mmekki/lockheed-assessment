using BookStore.Api.Integrations.OpenLibrary;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Features.Discovery;

[ApiController]
[Route("api/v1/discovery/books")]
public sealed class DiscoveryController(IOpenLibraryClient openLibraryClient) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<BookSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<BookSearchResponse>> Search(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "A search query is required.");
        }

        if (page < 1 || pageSize is < 1 or > 50)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination values.",
                detail: "Page must be at least 1 and pageSize must be between 1 and 50.");
        }

        try
        {
            var result = await openLibraryClient.SearchAsync(
                query.Trim(),
                page,
                pageSize,
                cancellationToken);

            return Ok(new BookSearchResponse(
                query.Trim(),
                page,
                pageSize,
                result.Total,
                result.Books));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Open Library did not respond in time.");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Open Library is currently unavailable.");
        }
    }
}

public sealed record BookSearchResponse(
    string Query,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<OpenLibraryBook> Items);
