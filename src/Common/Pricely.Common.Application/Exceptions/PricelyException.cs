using Pricely.Common.Domain;

namespace Pricely.Common.Application.Exceptions;

public sealed class PricelyException : Exception
{
    public PricelyException(string requestName, Error? error = default, Exception? innerException = default)
        : base("Application exception", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public Error? Error { get; }
}
