
using System.Linq;
using System.Text;
using Pricely.Common.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Pricely.Common.Infrastructure.Authentication;

internal static class AuthenticationExtensions
{
    internal static async Task<IServiceCollection> AddAuthenticationInternalAsync(this IServiceCollection services, IConfiguration configuration)
    {

        string jwtSecret = await configuration.GetSecretValueFromPathAsync("Jwt:Secret")
                    ?? throw new InvalidOperationException("JWT Secret not configured");

        services.AddAuthorization();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(async options =>
            {
                ServiceProvider serviceProvider = services.BuildServiceProvider();
                IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
                IConfigurationSection jwtSettings = configuration.GetSection("Jwt");
                 

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();

        services.ConfigureOptions<JwtBearerConfigureOptions>();

        return services;
    }
}
