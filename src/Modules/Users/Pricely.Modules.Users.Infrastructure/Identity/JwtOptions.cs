namespace Pricely.Modules.Users.Infrastructure.Identity;

public sealed class JwtOptions
{    
    public string Secret { get; init; }
    public string Issuer { get; init; } 
    public string Audience { get; init; }
    public int ExpirationInMinutes { get; init; } = 60;
    public int RefreshTokenExpirationInDays { get; init; } = 7;
}
