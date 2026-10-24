using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Users.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Pricely.Modules.Users.Presentation.Users;

internal sealed class GenerateAccessToken() : IEndpoint
{

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/access-token", async (
            TokenRequest request,
            IIdentityProviderService identityProvider,
            CancellationToken cancellationToken) =>
        {
            // Valider la requête
            if (string.IsNullOrWhiteSpace(request.Credential))
            {
                return Results.BadRequest(new { Error = "Credential is required" });
            }
              

            Result<string> result = await identityProvider.GenerateAccessTokenAsync(
                credential: request.Credential,
                cancellationToken: cancellationToken);

            if (result.IsFailure)
            {
                return Results.Unauthorized();
            }

            // Retourner le token JWT
            return Results.Ok(new AccessTokenResponse(
                AccessToken: result.Value,
                TokenType: "Bearer",
                ExpiresIn: 3600 // 1 heure en secondes
            ));
        })
        .AllowAnonymous() // Pas d'authentification requise pour l'auth initiale
        .WithTags(Tags.Users)
        .WithName("AccessToken")
        .WithDescription("Generate and receive a JWT token if credential is authorized");
    }

  
}

public record TokenRequest(
    string Credential);

public record AccessTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);
