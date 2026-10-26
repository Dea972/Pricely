using Microsoft.AspNetCore.Components;
using Polly;
using Polly.Timeout;
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
            // A success does not clear the message of an earlier failure: each user action clears it first.
            return await call();
        }
        catch (Exception exception) when (ApiProblem.From(exception, LoginAddress) is ApiProblem problem)
        {
            Problem = problem;

            return null;
        }
    }

    /// <summary>
    /// A page that Pricely could not read (site that does not exist, out of reach or that refuses robots): said in
    /// words instead of the technical message of the API.
    /// </summary>
    protected void ExplainUnreadablePage()
    {
        if (Problem?.Exception?.Code is "Web.Unreadable" or "Web.TooManyRedirects" or "Web.RedirectNotAllowed")
        {
            Problem = new ApiProblem("La page n'a pas pu être lue : adresse inexistante, site injoignable ou qui refuse les robots. Vérifie l'adresse, ou saisis à la main.");
        }
    }

    protected string LoginAddress => "authentication/login?returnUrl=" + Uri.EscapeDataString("/" + Navigation.ToBaseRelativePath(Navigation.Uri));
}

/// <summary>What the user sees of a failed call.</summary>
public sealed record ApiProblem(string Message, string? LoginAddress = null, PricelyApiException? Exception = null)
{
    public bool IsSessionExpired => LoginAddress is not null;

    public bool IsTimeout { get; private init; }

    /// <summary>The problem of a known failure, or null for a bug (left to the error boundary).</summary>
    public static ApiProblem? From(Exception exception, string loginAddress) =>
        exception switch
        {
            SessionExpiredException => new ApiProblem("Ta session a expiré.", loginAddress),
            PricelyApiException api => new ApiProblem($"La demande a échoué : {api.Message}", Exception: api),
            HttpRequestException => new ApiProblem("L'API Pricely ne répond pas. Réessaie dans un instant."),

            // Timeout of the resilience pipeline or open circuit breaker; a TaskCanceledException that nobody asked for
            // is the timeout of the HTTP client.
            TimeoutRejectedException or TaskCanceledException => new ApiProblem("L'API Pricely n'a pas répondu à temps.") { IsTimeout = true },
            ExecutionRejectedException => new ApiProblem("L'API Pricely ne répond pas. Réessaie dans un instant."),
            _ => null
        };
}
