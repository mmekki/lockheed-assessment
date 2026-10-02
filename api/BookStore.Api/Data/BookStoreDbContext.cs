using BookStore.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Data;

public sealed class BookStoreDbContext(DbContextOptions<BookStoreDbContext> options)
    : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookStoreDbContext).Assembly);
    }
}
