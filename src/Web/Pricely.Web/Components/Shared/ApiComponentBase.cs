using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Authentication;

namespace Pricely.Web.Components.Shared;

/// <summary>
/// A component that calls the API: a refused call becomes a message (<see cref="Problem"/>), an expired session a link to
/// sign in again, instead of an unhandled exception that would end the circuit.
/// </summary>
public abstract class ApiComponentBase : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected ApiProblem? Problem { get; set; }

    protected async Task<T?> CallAsync<T>(Func<Task<T>> call)
        where T : class
    {
        try
        {
            T result = await call();
            Problem = null;

            return result;
        }
        catch (Exception exception) when (ApiProblem.From(exception, LoginAddress) is ApiProblem problem)
        {
            Problem = problem;

            return null;
        }
    }

    protected string LoginAddress => "authentication/login?returnUrl=" + Uri.EscapeDataString("/" + Navigation.ToBaseRelativePath(Navigation.Uri));
}

/// <summary>What the user sees of a failed call.</summary>
public sealed record ApiProblem(string Message, string? LoginAddress = null, PricelyApiException? Exception = null)
{
    public bool IsSessionExpired => LoginAddress is not null;

    /// <summary>The problem of a known failure, or null for a bug (left to the error boundary).</summary>
    public static ApiProblem? From(Exception exception, string loginAddress) =>
        exception switch
        {
            SessionExpiredException => new ApiProblem("Ta session a expiré.", loginAddress),
            PricelyApiException api => new ApiProblem($"La demande a échoué : {api.Message}", Exception: api),
            HttpRequestException => new ApiProblem("L'API Pricely ne répond pas. Réessaie dans un instant."),
            _ => null
        };
}
