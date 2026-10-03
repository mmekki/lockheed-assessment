namespace BookStore.Application.Abstractions.Catalog;

public class CatalogUnavailableException : Exception
{
    public CatalogUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class CatalogTimeoutException : CatalogUnavailableException
{
    public CatalogTimeoutException(Exception innerException)
        : base("The book catalog did not respond in time.", innerException)
    {
    }
}
