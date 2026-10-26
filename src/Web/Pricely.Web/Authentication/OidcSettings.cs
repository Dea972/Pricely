namespace Pricely.Web.Authentication;

/// <summary>
/// Keycloak client of the front (section <c>Authentication</c>): a public client with PKCE (ADR-010, ADR-015).
/// </summary>
internal sealed class OidcSettings
{
    public const string SectionName = "Authentication";

    public string Authority { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; init; } = [];
}
