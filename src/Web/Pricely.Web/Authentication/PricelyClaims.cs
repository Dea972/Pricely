namespace Pricely.Web.Authentication;

internal static class PricelyClaims
{
    /// <summary>Key of the server-side tokens of the session (<see cref="ServerTokenStore"/>), added at sign-in.</summary>
    public const string Session = "pricely_session";

    public const string Name = "name";

    public const string PreferredUserName = "preferred_username";
}
