using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Modules.Agentic.Application;

namespace Pricely.Modules.Agentic.Presentation.Agents;

internal static class AgenticAccess
{
    /// <summary>
    /// The policy chosen by the application (<see cref="AgenticOptions.AccessPolicy"/>), or any authenticated caller:
    /// the module does not know the application's scopes.
    /// </summary>
    public static TBuilder RequireAgenticAccess<TBuilder>(this TBuilder builder, IEndpointRouteBuilder app)
        where TBuilder : IEndpointConventionBuilder
    {
        string? policy = app.ServiceProvider.GetRequiredService<AgenticOptions>().AccessPolicy;

        return policy is null ? builder.RequireAuthorization() : builder.RequireAuthorization(policy);
    }
}
