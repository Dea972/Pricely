namespace Pricely.Modules.Agentic.Application.Abstractions.Data;

/// <summary>
/// A conversation or an action was changed by another request between its read and its save.
/// </summary>
public sealed class ConcurrentUpdateException : Exception
{
    public ConcurrentUpdateException()
    {
    }

    public ConcurrentUpdateException(string message)
        : base(message)
    {
    }

    public ConcurrentUpdateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
