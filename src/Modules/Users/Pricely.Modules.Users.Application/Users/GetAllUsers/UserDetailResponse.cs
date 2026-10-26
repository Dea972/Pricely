namespace Pricely.Modules.Users.Application.Users.GetAllUsers;

/// <summary>
/// Réponse détaillée d'un utilisateur avec permissions et rôle (pour admin)
/// </summary>
public sealed record UserDetailResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    IEnumerable<string> Permissions);
