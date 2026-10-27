namespace Pricely.Common.Infrastructure.Web.Search;

/// <summary>A failed search that the provider may still have billed (<see cref="BilledSearches"/>), for the quota and the journal.</summary>
public sealed class WebSearchFailedException : Exception
{
    public WebSearchFailedException()
    {
    }

    public WebSearchFailedException(string message)
        : base(message)
    {
    }

    public WebSearchFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public WebSearchFailedException(string message, int billedSearches, Exception? innerException = null)
        : base(message, innerException) =>
        BilledSearches = billedSearches;

    /// <summary>Searches the provider billed before the failure.</summary>
    public int BilledSearches { get; }
}
