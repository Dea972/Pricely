namespace Pricely.Common.Infrastructure.Authentication;

public static class CustomClaims
{
    /// <summary>Identity id given by the identity provider (the user's <c>IdentityId</c>).</summary>
    public const string Sub = "sub";

    /// <summary>Internal Pricely user id, added by the claims transformation.</summary>
    public const string UserId = "pricely_user_id";

    public const string Permission = "permission";

    public const string Scope = "scope";

    public const string Email = "email";

    public const string GivenName = "given_name";

    public const string FamilyName = "family_name";

    public const string PreferredUserName = "preferred_username";
}
