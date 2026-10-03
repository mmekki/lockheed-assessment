namespace BookStore.Application.Abstractions.Persistence;

public sealed class DuplicateBookException : Exception
{
    public DuplicateBookException(Exception? innerException = null)
        : base("A book with the same Open Library key already exists.", innerException)
    {
    }
}
