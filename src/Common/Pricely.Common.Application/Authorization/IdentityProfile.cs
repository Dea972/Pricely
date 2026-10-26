namespace Pricely.Common.Application.Authorization;

/// <summary>
/// What the identity provider says about the caller, read from the access token (<c>sub</c> is the identity id).
/// </summary>
public sealed record IdentityProfile(string IdentityId, string? Email, string? FirstName, string? LastName, string? UserName);
