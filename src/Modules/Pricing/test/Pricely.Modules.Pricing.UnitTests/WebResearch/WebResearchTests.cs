using FluentAssertions;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Merchants;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Application.WebResearch.ReadWebPage;
using Pricely.Modules.Pricing.Application.WebResearch.SearchWeb;

namespace Pricely.Modules.Pricing.UnitTests.WebResearch;

public sealed class WebResearchTests
{
    private static readonly MerchantResponse Comptoir = new(Guid.NewGuid(), "Comptoir", "https://www.comptoir.test/", true, null, null);

    [Fact]
    public async Task Search_ShouldMarkThePagesOfAKnownMerchant_AndJournalTheUser()
    {
        var userId = Guid.NewGuid();
        var searcher = new FakeSearcher(new WebSearchResults(
            WebSearchProviders.Brave,
            [
                new WebSearchResult("Ciment 35 kg", new Uri("https://comptoir.test/p/ciment"), "9,90 €", null),
                new WebSearchResult("Forum", new Uri("https://forum.test/ciment"), null, "2 days ago")
            ],
            1));

        Result<WebSearchResponse> result = await new SearchWebQueryHandler(searcher, new FakeMerchantReadStore(Comptoir), new FakeCurrentUser(userId))
            .Handle(new SearchWebQuery("prix ciment 35 kg"), CancellationToken.None);

        result.Value.Provider.Should().Be(WebSearchProviders.Brave);
        result.Value.Results.Select(r => r.Merchant?.Name).Should().Equal("Comptoir", null);
        result.Value.Notice.Should().Contain("non vérifiés");
        searcher.LastRequest!.UserId.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task Search_ShouldReturnTheSearcherFailure()
    {
        Result<WebSearchResponse> result = await new SearchWebQueryHandler(new FakeSearcher(null), new FakeMerchantReadStore(), new FakeCurrentUser(null))
            .Handle(new SearchWebQuery("ciment"), CancellationToken.None);

        result.Error.Code.Should().Be(WebSearchErrors.DailyLimitReached(1).Code);
    }

    [Fact]
    public async Task Read_ShouldFollowAWebRedirect_AndReturnTheDeclaredProducts()
    {
        var web = new FakeWeb();
        web.Pages["https://comptoir.test/p/1"] = new WebPage(new Uri("https://comptoir.test/p/1"), string.Empty, new Uri("https://www.comptoir.test/p/1"));
        web.Pages["https://www.comptoir.test/p/1"] = new WebPage(new Uri("https://www.comptoir.test/p/1"), new string('x', WebPageResponse.MaxTextLength + 5), null)
        {
            Title = "Ciment",
            Products = [new StructuredProduct("Ciment 35 kg", "3760000100014", "Cimenterie", "C35", [new StructuredOffer(9.9m, "EUR", "InStock", null)])]
        };

        Result<WebPageResponse> result = await new ReadWebPageQueryHandler(web, new FakeMerchantReadStore(Comptoir))
            .Handle(new ReadWebPageQuery(new Uri("https://comptoir.test/p/1")), CancellationToken.None);

        result.Value.Address.Should().Be(new Uri("https://www.comptoir.test/p/1"));
        result.Value.IsTruncated.Should().BeTrue();
        result.Value.Text.Should().HaveLength(WebPageResponse.MaxTextLength);
        result.Value.Products.Should().ContainSingle().Which.Offers.Should().ContainSingle().Which.Price.Should().Be(9.9m);
        result.Value.Merchant!.Id.Should().Be(Comptoir.Id);
    }

    [Fact]
    public async Task Read_ShouldRefuseARedirectOutsideTheWeb()
    {
        var web = new FakeWeb();
        web.Pages["https://site.test/"] = new WebPage(new Uri("https://site.test/"), string.Empty, new Uri("file:///etc/passwd"));

        Result<WebPageResponse> result = await new ReadWebPageQueryHandler(web, new FakeMerchantReadStore())
            .Handle(new ReadWebPageQuery(new Uri("https://site.test/")), CancellationToken.None);

        result.Error.Code.Should().Be("Web.RedirectNotAllowed");
    }

    private sealed class FakeSearcher(WebSearchResults? results) : IWebSearcher
    {
        public WebSearchRequest? LastRequest { get; private set; }

        public Task<Result<WebSearchResults>> SearchAsync(WebSearchRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;

            return Task.FromResult(results is null
                ? Result.Failure<WebSearchResults>(WebSearchErrors.DailyLimitReached(1))
                : Result.Success(results));
        }
    }

    private sealed class FakeMerchantReadStore(params MerchantResponse[] merchants) : IMerchantReadStore
    {
        public Task<IReadOnlyCollection<MerchantResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<MerchantResponse>>(merchants);
    }

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId => userId;

        public IReadOnlySet<string> Scopes { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    private sealed class FakeWeb : IWebPageReader
    {
        public Dictionary<string, WebPage> Pages { get; } = new(StringComparer.Ordinal);

        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default) =>
            Task.FromResult(Pages.TryGetValue(address.AbsoluteUri, out WebPage? page)
                ? Result.Success(page)
                : Result.Failure<WebPage>(WebErrors.Unreadable(address, "HTTP 404")));
    }
}
