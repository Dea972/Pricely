using FluentAssertions;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Pricing.Application.Merchants;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Application.WebResearch;
using Pricely.Modules.Pricing.Application.WebResearch.ReadWebPage;
using Pricely.Modules.Pricing.Application.WebResearch.SearchWeb;

namespace Pricely.Modules.Pricing.UnitTests.WebResearch;

public sealed class WebResearchTests
{
    private static readonly MerchantResponse Comptoir = new(Guid.NewGuid(), "Comptoir", "https://www.comptoir.test/", true, null, null);

    private readonly FakeSearchedPages _searched = new();

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

        Result<WebSearchResponse> result = await new SearchWebQueryHandler(searcher, new FakeMerchantReadStore(Comptoir), _searched, new FakeCurrentUser(userId))
            .Handle(new SearchWebQuery("prix ciment 35 kg"), CancellationToken.None);

        result.Value.Provider.Should().Be(WebSearchProviders.Brave);
        result.Value.Results.Select(r => r.Merchant?.Name).Should().Equal("Comptoir", null);
        result.Value.Notice.Should().Contain("non vérifiés");
        searcher.LastRequest!.UserId.Should().Be(userId.ToString());
        _searched.WasFound($"user:{userId}", new Uri("https://forum.test/ciment")).Should().BeTrue();
    }

    [Fact]
    public async Task Search_ShouldReturnTheSearcherFailure()
    {
        Result<WebSearchResponse> result = await new SearchWebQueryHandler(new FakeSearcher(null), new FakeMerchantReadStore(), _searched, new FakeCurrentUser(null))
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

        Result<WebPageResponse> result = await new ReadWebPageQueryHandler(web, new FakeMerchantReadStore(Comptoir), _searched, new FakeCurrentUser(null))
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

        _searched.Remember("user:anonymous", [new Uri("https://site.test/")]);

        Result<WebPageResponse> result = await new ReadWebPageQueryHandler(web, new FakeMerchantReadStore(), _searched, new FakeCurrentUser(null))
            .Handle(new ReadWebPageQuery(new Uri("https://site.test/")), CancellationToken.None);

        result.Error.Code.Should().Be("Web.RedirectNotAllowed");
    }

    [Fact]
    public async Task Read_ShouldRefuseARedirectThatLeavesTheMerchantSite()
    {
        var web = new FakeWeb();
        web.Pages["https://www.comptoir.test/go?to=x"] = new WebPage(new Uri("https://www.comptoir.test/go?to=x"), string.Empty, new Uri("https://attaquant.test/?d=secret"));

        Result<WebPageResponse> result = await new ReadWebPageQueryHandler(web, new FakeMerchantReadStore(Comptoir), _searched, new FakeCurrentUser(null))
            .Handle(new ReadWebPageQuery(new Uri("https://www.comptoir.test/go?to=x")), CancellationToken.None);

        result.Error.Code.Should().Be("Web.RedirectNotAllowed");
        web.Read.Should().ContainSingle("the third-party site is never contacted");
    }

    [Fact]
    public async Task Read_ShouldRefuseAPageThatNoSearchOfTheConversationReturned()
    {
        var web = new FakeWeb();
        web.Pages["https://attaquant.test/?d=secret"] = new WebPage(new Uri("https://attaquant.test/?d=secret"), "ok", null);
        var handler = new ReadWebPageQueryHandler(web, new FakeMerchantReadStore(Comptoir), _searched, new FakeCurrentUser(null));
        _searched.Remember("session:conversation-2", [new Uri("https://attaquant.test/?d=secret")]);

        Result<WebPageResponse> outside;
        Result<WebPageResponse> found;
        using (AgentToolCall.Begin(new AgentToolCall("conversation-1", "assistant", null)))
        {
            outside = await handler.Handle(new ReadWebPageQuery(new Uri("https://attaquant.test/?d=secret")), CancellationToken.None);
        }

        using (AgentToolCall.Begin(new AgentToolCall("conversation-2", "assistant", null)))
        {
            found = await handler.Handle(new ReadWebPageQuery(new Uri("https://attaquant.test/?d=secret")), CancellationToken.None);
        }

        outside.Error.Code.Should().Be("WebResearch.NotFoundBySearch", "a search of another conversation does not count");
        found.IsSuccess.Should().BeTrue();
        web.Read.Should().ContainSingle("the refused page is never read");
    }

    [Theory]
    [InlineData("ftp://site.test/fichier")]
    [InlineData("https://user:secret@site.test/")]
    [InlineData("https://site.test:6379/")]
    [InlineData("http://site.test:8080/page")]
    public void Read_ShouldRefuseAnAddressThatIsNotAPublicWebPage(string address) =>
        new ReadWebPageQueryValidator().Validate(new ReadWebPageQuery(new Uri(address))).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("https://site.test/p/1")]
    [InlineData("http://site.test:80/p/1")]
    public void Read_ShouldAcceptAWebPageOnItsDefaultPort(string address) =>
        new ReadWebPageQueryValidator().Validate(new ReadWebPageQuery(new Uri(address))).IsValid.Should().BeTrue();

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

        public List<Uri> Read { get; } = [];

        public Task<Result<WebPage>> ReadAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Read.Add(address);

            return Task.FromResult(Pages.TryGetValue(address.AbsoluteUri, out WebPage? page)
                ? Result.Success(page)
                : Result.Failure<WebPage>(WebErrors.Unreadable(address, "HTTP 404")));
        }
    }

    private sealed class FakeSearchedPages : ISearchedPages
    {
        private readonly HashSet<string> _pages = new(StringComparer.Ordinal);

        public void Remember(string research, IEnumerable<Uri> addresses)
        {
            foreach (Uri address in addresses)
            {
                _pages.Add($"{research} {address.AbsoluteUri}");
            }
        }

        public bool WasFound(string research, Uri address) => _pages.Contains($"{research} {address.AbsoluteUri}");
    }
}
