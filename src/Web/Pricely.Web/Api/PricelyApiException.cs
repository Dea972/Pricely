using System.Net;

namespace Pricely.Web.Api;

/// <summary>
/// The API refused a call: <see cref="Code"/> is the error code of the module (title of the problem details), for
/// instance <c>Actions.AlreadyDecided</c>.
/// </summary>
public sealed class PricelyApiException : Exception
{
    public PricelyApiException()
    {
    }

    public PricelyApiException(string message)
        : base(message)
    {
    }

    public PricelyApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PricelyApiException(HttpStatusCode statusCode, string? code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public HttpStatusCode StatusCode { get; }

    public string? Code { get; }
}
