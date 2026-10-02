using System.Text.Json.Serialization;
using BookStore.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddDbContext<BookStoreDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("BookStore")
        ?? throw new InvalidOperationException("The BookStore database connection is not configured.")));
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
