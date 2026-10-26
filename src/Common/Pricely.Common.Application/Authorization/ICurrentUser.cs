namespace Pricely.Common.Application.Authorization;

/// <summary>
/// The caller of the current request, for modules that need it outside an endpoint (for example the Agentic module).
/// </summary>
public interface ICurrentUser
{
    /// <summary>Internal user id; null when the caller is anonymous or unknown to the application.</summary>
    Guid? UserId { get; }

    /// <summary>OAuth scopes of the caller's token.</summary>
    IReadOnlySet<string> Scopes { get; }
}
