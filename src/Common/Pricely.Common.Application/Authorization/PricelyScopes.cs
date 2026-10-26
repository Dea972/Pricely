namespace Pricely.Common.Application.Authorization;

/// <summary>
/// OAuth scopes issued by the identity provider (ADR-010). Each scope is also the name of an authorization policy:
/// <c>.RequireAuthorization(PricelyScopes.Read)</c>.
/// </summary>
public static class PricelyScopes
{
    /// <summary>Consultation of products, offers and prices.</summary>
    public const string Read = "pricely.read";

    /// <summary>Administration and price collection.</summary>
    public const string Write = "pricely.write";

    /// <summary>Confirmation or rejection of proposed offers.</summary>
    public const string Review = "pricely.review";

    public static IReadOnlyCollection<string> All { get; } = [Read, Write, Review];
}
