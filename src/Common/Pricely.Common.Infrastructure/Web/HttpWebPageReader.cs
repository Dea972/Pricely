using System.Net;
using System.Text.RegularExpressions;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;

namespace Pricely.Common.Infrastructure.Web;

/// <summary>
/// Reads a web page over HTTP (merchant pages, product pages, pricing pages of the LLM providers): no automatic redirect
/// (the caller decides where it may go), public addresses only (<see cref="PublicAddressGuard"/>), 3 MB at most, its markup
/// reduced to text (<see cref="HtmlText"/>) with the search pages it declares.
/// </summary>
internal sealed class HttpWebPageReader(IHttpClientFactory httpClientFactory) : IWebPageReader
{
    public const string HttpClientName = "pricely-web-pages";

    public const string UserAgent = "PricelyBot/1.0 (+https://github.com/Dea972/Pricely)";

    public const int MaxPageBytes = 3 * 1024 * 1024;

    public async Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default)
    {
        HttpClient http = httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, address);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.5");

            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is Uri location)
            {
                return new WebPage(address, string.Empty, location.IsAbsoluteUri ? location : new Uri(address, location));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<WebPage>(WebErrors.Unreadable(address, $"HTTP {(int)response.StatusCode}"));
            }

            if (response.Content.Headers.ContentLength > MaxPageBytes)
            {
                return Result.Failure<WebPage>(WebErrors.Unreadable(address, "page too large"));
            }

            await response.Content.LoadIntoBufferAsync(MaxPageBytes, cancellationToken);
            string html = await response.Content.ReadAsStringAsync(cancellationToken);

            return new WebPage(address, HtmlText.Extract(html), RedirectTo: null)
            {
                Title = HtmlText.Title(html),
                SearchTemplates = HtmlText.SearchTemplates(html, address)
            };
        }
        catch (HttpRequestException exception)
        {
            return Result.Failure<WebPage>(WebErrors.Unreadable(address, exception.Message));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<WebPage>(WebErrors.Unreadable(address, "timeout"));
        }
        catch (RegexMatchTimeoutException)
        {
            return Result.Failure<WebPage>(WebErrors.Unreadable(address, "page too complex to read"));
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
