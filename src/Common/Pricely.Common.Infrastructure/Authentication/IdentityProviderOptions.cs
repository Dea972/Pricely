using System.ComponentModel.DataAnnotations;

namespace Pricely.Common.Infrastructure.Authentication;

/// <summary>
/// The authorization server whose JWT access tokens the API accepts (Keycloak, ADR-010).
/// Bound from the <c>Authentication</c> section; under Aspire the AppHost sets <c>Authentication__Authority</c>.
/// </summary>
public sealed class IdentityProviderOptions
{
    public const string SectionName = "Authentication";

    /// <summary>Issuer of the tokens, e.g. <c>http://localhost:8080/realms/pricely</c>; metadata is discovered from it.</summary>
    [Required]
    public Uri? Authority { get; init; }

    /// <summary>Expected <c>aud</c> claim of the access tokens.</summary>
    [Required]
    public string Audience { get; init; } = "pricely-api";

    /// <summary>Set to <c>false</c> only for a local identity provider served over HTTP.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}
