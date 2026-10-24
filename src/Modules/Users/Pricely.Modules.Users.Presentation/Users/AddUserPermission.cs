using Pricely.Common.Domain;
using Pricely.Common.Presentation.ApiResults;
using Pricely.Common.Presentation.Endpoints;
using Pricely.Modules.Users.Application.Users.AddUserPermission;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Pricely.Modules.Users.Presentation.Users;

internal sealed class AddUserPermission : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/{id:guid}/permissions", async (Guid id, AddUserPermissionRequest request, ISender sender) =>
        {
            var command = new AddUserPermissionCommand(id, request.PermissionCode);

            Result result = await sender.Send(command);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        })
        .RequireAuthorization(Permissions.AdminAccess)
        .WithTags(Tags.Users);
    }

    internal sealed record AddUserPermissionRequest(string PermissionCode);
}
