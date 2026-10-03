using System.Text.Json.Serialization;
using BookStore.Application.Abstractions.Catalog;
using BookStore.Application.Abstractions.Messaging;
using BookStore.Application.Abstractions.Persistence;
using BookStore.Application.Features.Books.AddBook;
using BookStore.Application.Features.Books.DeleteBook;
using BookStore.Application.Features.Books.GetBook;
using BookStore.Application.Features.Books.GetBookCover;
using BookStore.Application.Features.Books.GetBooks;
using BookStore.Application.Features.Books.UpdateBook;
using BookStore.Application.Features.Catalog.SearchCatalog;
using BookStore.Domain;
using BookStore.Infrastructure.Catalog;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<IRequestHandler<GetBooksQuery, IReadOnlyList<Book>>, GetBooksHandler>();
builder.Services.AddScoped<IRequestHandler<GetBookQuery, Book?>, GetBookHandler>();
builder.Services.AddScoped<IRequestHandler<GetBookCoverQuery, BookCover?>, GetBookCoverHandler>();
builder.Services.AddScoped<IRequestHandler<AddBookCommand, AddBookResult>, AddBookHandler>();
builder.Services.AddScoped<IRequestHandler<UpdateBookCommand, UpdateBookResult>, UpdateBookHandler>();
builder.Services.AddScoped<IRequestHandler<DeleteBookCommand, bool>, DeleteBookHandler>();
builder.Services.AddScoped<
    IRequestHandler<SearchCatalogQuery, CatalogSearchPage>,
    SearchCatalogHandler>();

builder.Services.AddDbContext<BookStoreDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("BookStore")
        ?? throw new InvalidOperationException("The BookStore database connection is not configured.")));
builder.Services.AddScoped<IBookRepository, EfBookRepository>();
builder.Services.AddHttpClient<IBookCatalog, OpenLibraryClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["OpenLibrary:BaseUrl"]
        ?? throw new InvalidOperationException("The Open Library base URL is not configured."));
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BookStoreAssessment/1.0");
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("WebClient");
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapGet("/health", () => TypedResults.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithTags("System");

app.Run();
