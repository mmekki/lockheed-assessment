var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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
