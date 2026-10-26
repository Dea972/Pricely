namespace Pricely.Web.Authentication;

/// <summary>The tokens of a signed-in user, kept on the server only.</summary>
internal sealed record TokenSet(string AccessToken, string? RefreshToken, string? IdToken, DateTimeOffset ExpiresAt);
