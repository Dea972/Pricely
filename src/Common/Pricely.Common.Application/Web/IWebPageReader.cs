using Pricely.Common.Domain;

namespace Pricely.Common.Application.Web;

/// <summary>
/// Port to the web: reads one page and returns its text. Connects to public addresses only (never the loopback, a private
/// network or a cloud metadata address) and never follows a redirect itself: the caller decides where it may go
/// (<see cref="WebPageReaderExtensions.ReadFollowingAsync"/>). Implemented in Common.Infrastructure.
/// </summary>
public interface IWebPageReader
{
    Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default);
}

/// <param name="Address">The address that was read.</param>
/// <param name="Text">The readable text of the page (markup removed, structured data kept).</param>
/// <param name="RedirectTo">Where the site redirects, when it answers with a redirect instead of a page.</param>
public sealed record WebPage(Uri Address, string Text, Uri? RedirectTo)
{
    /// <summary>The title of the page (<c>&lt;title&gt;</c> or <c>og:site_name</c>), if any.</summary>
    public string? Title { get; init; }

    /// <summary>
    /// Search pages that the markup declares (search forms, <c>SearchAction</c> of the structured data), as absolute
    /// addresses where <c>{query}</c> stands for the searched text.
    /// </summary>
    public IReadOnlyList<string> SearchTemplates { get; init; } = [];

    /// <summary>The products the page declares in its structured data (JSON-LD), with their prices.</summary>
    public IReadOnlyList<StructuredProduct> Products { get; init; } = [];
}

public static class WebErrors
{
    public static Error Unreadable(Uri address, string reason) =>
        Error.Problem("Web.Unreadable", $"The page {address} could not be read: {reason}");

    public static Error TooManyRedirects(Uri address) =>
        Error.Problem("Web.TooManyRedirects", $"The page {address} redirects too many times");

    public static Error RedirectNotAllowed(Uri address) =>
        Error.Problem("Web.RedirectNotAllowed", $"The page redirects to {address}, which is not allowed");
}

public static class WebPageReaderExtensions
{
    public const int MaxRedirects = 3;

    /// <summary>Reads a page, following at most <see cref="MaxRedirects"/> redirects that <paramref name="isAllowed"/> accepts.</summary>
    public static async Task<Result<WebPage>> ReadFollowingAsync(
        this IWebPageReader reader,
        Uri address,
        Func<Uri, bool> isAllowed,
        CancellationToken cancellationToken = default)
    {
        Uri target = address;
        for (int redirects = 0; redirects <= MaxRedirects; redirects++)
        {
            Result<WebPage> page = await reader.ReadAsync(target, cancellationToken);
            if (page.IsFailure || page.Value.RedirectTo is not Uri next)
            {
                return page;
            }

            if (!isAllowed(next))
            {
                return Result.Failure<WebPage>(WebErrors.RedirectNotAllowed(next));
            }

            target = next;
        }

        return Result.Failure<WebPage>(WebErrors.TooManyRedirects(target));
    }
}
