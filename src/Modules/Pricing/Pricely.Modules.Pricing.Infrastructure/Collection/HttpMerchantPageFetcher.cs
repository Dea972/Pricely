using System.Net;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Abstractions.Collection;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Infrastructure.Collection;

/// <summary>
/// Reads a merchant page over HTTP (anti-corruption layer): no automatic redirect (the caller checks the target is on
/// the merchant's site), public addresses only (<see cref="PublicAddressGuard"/>), 3 MB at most, text only.
/// </summary>
internal sealed class HttpMerchantPageFetcher(IHttpClientFactory httpClientFactory) : IMerchantPageFetcher
{
    public const string HttpClientName = "pricing-merchant-pages";

    public const string UserAgent = "PricelyBot/1.0 (+https://github.com/Dea972/Pricely)";

    public const int MaxPageBytes = 3 * 1024 * 1024;

    public async Task<Result<FetchedPage>> FetchAsync(Uri address, CancellationToken cancellationToken = default)
    {
        HttpClient http = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, address);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.5");

            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is Uri location)
            {
                return new FetchedPage(address, string.Empty, location.IsAbsoluteUri ? location : new Uri(address, location));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<FetchedPage>(PageSnapshotErrors.Unreadable(address, $"HTTP {(int)response.StatusCode}"));
            }

            if (response.Content.Headers.ContentLength > MaxPageBytes)
            {
                return Result.Failure<FetchedPage>(PageSnapshotErrors.Unreadable(address, "page too large"));
            }

            await response.Content.LoadIntoBufferAsync(MaxPageBytes, cancellationToken);
            string html = await response.Content.ReadAsStringAsync(cancellationToken);

            return new FetchedPage(address, HtmlText.Extract(html), RedirectTo: null);
        }
        catch (HttpRequestException exception)
        {
            return Result.Failure<FetchedPage>(PageSnapshotErrors.Unreadable(address, exception.Message));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<FetchedPage>(PageSnapshotErrors.Unreadable(address, "timeout"));
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            return Result.Failure<FetchedPage>(PageSnapshotErrors.Unreadable(address, "page too complex to read"));
        }
    }

    /// <summary>
    /// The primary handler of the client: redirects and connections under Pricely's control. No proxy: through a proxy,
    /// the guard would see the address of the proxy instead of the one of the merchant site.
    /// </summary>
    public static SocketsHttpHandler CreateHandler() =>
        new()
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectCallback = PublicAddressGuard.ConnectAsync,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
}
