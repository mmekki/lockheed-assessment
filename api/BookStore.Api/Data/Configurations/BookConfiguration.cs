using BookStore.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookStore.Api.Data.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Books_Price", "PriceInCents >= 0");
            tableBuilder.HasCheckConstraint("CK_Books_Stock", "Stock IS NULL OR Stock >= 0");
        });

        builder.HasKey(book => book.Id);

        builder.Property(book => book.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(book => book.Author)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(book => book.Isbn)
            .HasMaxLength(20);

        builder.Property(book => book.CoverUrl)
            .HasMaxLength(1_000);

        builder.Property(book => book.CoverContentType)
            .HasMaxLength(100);

        builder.Property(book => book.OpenLibraryKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(book => book.OpenLibraryKey)
            .IsUnique();

        builder.Property(book => book.Description)
            .HasMaxLength(5_000);

        builder.Property(book => book.Genre)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(book => book.Price)
            .HasColumnName("PriceInCents")
            .HasConversion(
                price => decimal.ToInt64(price * 100m),
                cents => cents / 100m)
            .IsRequired();
    }
}
