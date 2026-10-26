namespace Pricely.Web.Authentication;

/// <summary>
/// The tokens of the session are gone (server restarted, refresh refused by Keycloak): the user has to sign in again.
/// </summary>
public sealed class SessionExpiredException : Exception
{
    public SessionExpiredException()
        : base("The session has expired: sign in again.")
    {
    }

    public SessionExpiredException(string message)
        : base(message)
    {
    }

    public SessionExpiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
