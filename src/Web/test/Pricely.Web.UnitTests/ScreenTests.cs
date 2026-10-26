using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Web.Api;
using Pricely.Web.Components.Pages;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.UnitTests;

/// <summary>The screens of step 5b: offers to review, collection profiles, AI and costs.</summary>
public sealed class ScreenTests : BunitContext
{
    private static readonly Guid OfferId = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67201");
    private static readonly Guid Root = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67301");
    private static readonly Guid Cement = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67302");
    private static readonly Guid Comptoir = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67401");
    private static readonly Guid Atlantique = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67402");
    private static readonly Guid Haiku = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67501");

    private readonly TestSession _session = new TestSession().SignedIn(TimeSpan.FromHours(1));

    [Fact]
    public void Review_ShouldCompareTheReference_WithTheObservedPage_AndConfirm()
    {
        FakeServer api = Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/pricing/offers/pending-review") => Responses.Json(PendingOffers),
            ("POST", var path) when path.EndsWith("/confirm", StringComparison.Ordinal) => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<ReviewPage> page = Render<ReviewPage>();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Dépôt Matériaux Sud"));
        page.Markup.Should().Contain("Ciment gris CEM II 32,5R 35kg").And.Contain("✓ identique").And.Contain("non comparé")
            .And.Contain("Avis IA (forte, 90 %)").And.Contain("Même marque, même conditionnement.");
        Services.GetRequiredService<ReviewQueue>().Count.Should().Be(1);

        page.Find("button.btn-primary").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Aucune offre n'attend de validation.").And.Contain("1 traitée(s)"));
        api.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post)
            .Which.RequestUri!.AbsolutePath.Should().Be($"/api/pricing/offers/{OfferId}/confirm");
        Services.GetRequiredService<ReviewQueue>().Count.Should().Be(0, "the badge of the navigation follows");
    }

    [Fact]
    public void Review_WithoutTheReviewScope_ShouldSaySo_AndKeepTheOffer()
    {
        Api(request => request.Method == HttpMethod.Post
            ? Responses.Problem(HttpStatusCode.Forbidden, "Forbidden", "The 'pricely.review' scope is required.")
            : Responses.Json(PendingOffers));

        IRenderedComponent<ReviewPage> page = Render<ReviewPage>();
        page.WaitForAssertion(() => page.Markup.Should().Contain("Dépôt Matériaux Sud"));

        page.Find("button.btn-secondary").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Il te manque le droit de revue des offres (scope pricely.review)."));
        page.FindAll("article.review-card").Should().ContainSingle();
    }

    [Fact]
    public void Profiles_ShouldShowTheInheritedMerchants_AndThePromptBlockByBlock()
    {
        Api(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/catalog/categories" => Responses.Json(Categories),
            "/api/pricing/merchants" => Responses.Json(Merchants),
            "/api/pricing/collection-profiles" => Responses.Json($$"""[{"productCategoryId":"{{Root}}","categoryName":"Gros œuvre","preferredMerchantCount":2,"hasInstructions":true}]"""),
            var path when path.EndsWith("/effective", StringComparison.Ordinal) => Responses.Json(InheritedProfile),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<ProfilesPage> page = Render<ProfilesPage>(parameters => parameters.Add(p => p.CategoryId, Cement));

        page.WaitForAssertion(() => page.Markup.Should().Contain("Profil de collecte : Ciment"));
        page.Markup.Should().Contain("↳ Héritées de Gros œuvre").And.Contain("Gros œuvre › Ciment");
        page.FindAll(".merchant-order li").Select(li => li.TextContent).Should().SatisfyRespectively(
            first => first.Should().Contain("Comptoir du Bâtiment"),
            second => second.Should().Contain("Négoce Atlantique"));
        page.FindAll(".prompt-block").Select(b => b.TextContent).Should().SatisfyRespectively(
            root => root.Should().Contain("Gros œuvre").And.Contain("(hérité)").And.Contain("Prix TTC livré."),
            cement => cement.Should().Contain("Ciment").And.Contain("(cette typologie)").And.Contain("Prix au sac."));
        page.FindAll(".tree a").Should().HaveCount(2);
        page.Find(".tree a[aria-current='page']").TextContent.Should().Contain("Ciment");
    }

    [Fact]
    public void Profiles_ShouldSaveAnOwnMerchantList_InTheChosenOrder()
    {
        FakeServer api = Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/catalog/categories") => Responses.Json(Categories),
            ("GET", "/api/pricing/merchants") => Responses.Json(Merchants),
            ("GET", "/api/pricing/collection-profiles") => Responses.Json("[]"),
            ("GET", _) => Responses.Json(InheritedProfile),
            ("PUT", _) => Responses.Json($"\"{Guid.NewGuid()}\""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<ProfilesPage> page = Render<ProfilesPage>(parameters => parameters.Add(p => p.CategoryId, Cement));
        page.WaitForAssertion(() => page.Markup.Should().Contain("Définir une liste propre à Ciment"));

        page.FindAll("button").Single(b => b.TextContent.StartsWith("Définir une liste propre", StringComparison.Ordinal)).Click();
        page.Find("button[aria-label='Descendre Comptoir du Bâtiment']").Click();
        page.Find("button.btn-primary").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Profil enregistré."));
        int put = api.Requests.FindIndex(r => r.Method == HttpMethod.Put);
        api.Requests[put].RequestUri!.AbsolutePath.Should().Be($"/api/pricing/collection-profiles/{Cement}");
        api.Bodies[put].Should().Be($$"""{"preferredMerchantIds":["{{Atlantique}}","{{Comptoir}}"],"instructions":"Prix au sac."}""");
    }

    [Fact]
    public void AiUsage_ShouldShowTheCosts_AndAModelWithoutPrice()
    {
        Api(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/aiengine/usage/summary" => Responses.Json("""{"calls":12,"failedCalls":1,"unpricedCalls":2,"inputTokens":1500,"outputTokens":300,"totalTokens":1800,"cost":0.42}"""),
            "/api/aiengine/usage/daily" => Responses.Json("""[{"day":"2026-10-25T00:00:00Z","calls":12,"cost":0.42}]"""),
            "/api/aiengine/usage/by-feature" => Responses.Json("""[{"featureCode":"pricing.offer-collection","calls":12,"cost":0.42}]"""),
            "/api/aiengine/features" => Responses.Json(Features),
            "/api/aiengine/models" => Responses.Json(Models),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<AiUsagePage> page = Render<AiUsagePage>();

        page.WaitForAssertion(() => page.Markup.Should().Contain("0,4200 $"));
        page.Find(".stats").TextContent.Should().Contain("12").And.Contain($"{Format.Count(1500)} / 300");
        page.Markup.Should().Contain("⚠ aucun tarif : appels non chiffrés").And.Contain("hérite de default (claude-haiku-5-5)");
    }

    [Fact]
    public void AiUsage_ShouldTargetAModel_ForAFeature()
    {
        FakeServer api = Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/aiengine/features") => Responses.Json(Features),
            ("GET", "/api/aiengine/models") => Responses.Json(Models),
            ("GET", var path) when path.StartsWith("/api/aiengine/usage/summary", StringComparison.Ordinal) => Responses.Json("""{"calls":0,"cost":0}"""),
            ("GET", _) => Responses.Json("[]"),
            ("PUT", _) => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<AiUsagePage> page = Render<AiUsagePage>();
        page.WaitForAssertion(() => page.Markup.Should().Contain("pricing.offer-collection"));

        page.Find("#target-pricing\\.offer-collection").Change(Haiku.ToString());

        page.WaitForAssertion(() => api.Requests.Should().Contain(r => r.Method == HttpMethod.Put));
        int put = api.Requests.FindIndex(r => r.Method == HttpMethod.Put);
        api.Requests[put].RequestUri!.AbsolutePath.Should().Be("/api/aiengine/features/pricing.offer-collection/target");
        api.Bodies[put].Should().Be($$"""{"modelId":"{{Haiku}}"}""");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.Dispose();
        }

        base.Dispose(disposing);
    }

    private static readonly string PendingOffers = $$"""
        [{"offerId":"{{OfferId}}","productId":"{{Guid.NewGuid()}}","productName":"Ciment CEM II/B-LL 32,5 R","merchantName":"Dépôt Matériaux Sud",
          "productPage":"https://depot.test/p","proposedOnUtc":"2026-10-26T06:00:00Z","latestAmount":9.6,"latestCurrency":"EUR",
          "productBrand":"Cimenterie du Rhône","productGtin":"03456789012344","productManufacturerReference":"CR-325R-35",
          "observedTitle":"Ciment gris CEM II 32,5R 35kg","observedBrand":"Cimenterie du Rhône","aiSimilarity":0.9,
          "aiRationale":"Même marque, même conditionnement.","sameGtin":null,"sameBrand":true,"sameManufacturerReference":null}]
        """;

    private static readonly string Categories = $$"""
        [{"id":"{{Root}}","name":"Gros œuvre","parentId":null,"depth":1},{"id":"{{Cement}}","name":"Ciment","parentId":"{{Root}}","depth":2}]
        """;

    private static readonly string Merchants = $$"""
        [{"id":"{{Comptoir}}","name":"Comptoir du Bâtiment","website":"https://comptoir.test/","isActive":true},
         {"id":"{{Atlantique}}","name":"Négoce Atlantique","website":"https://negoce.test/","isActive":true}]
        """;

    private static readonly string InheritedProfile = $$"""
        {"preferredMerchantIds":["{{Comptoir}}","{{Atlantique}}"],"preferredMerchantsSourceCategoryId":"{{Root}}",
         "instructions":"Prix TTC livré.\n\nPrix au sac.",
         "instructionBlocks":[{"productCategoryId":"{{Root}}","instructions":"Prix TTC livré."},{"productCategoryId":"{{Cement}}","instructions":"Prix au sac."}],
         "hasOwnProfile":true,"ownPreferredMerchantIds":[],"ownInstructions":"Prix au sac."}
        """;

    private static readonly string Features = $$"""
        [{"code":"default","label":"Cible par défaut","sourceModule":null,"defaultProvider":null,"ownModelId":"{{Haiku}}","effectiveModelId":"{{Haiku}}","source":0},
         {"code":"pricing.offer-collection","label":"Collecte","sourceModule":"Pricing","defaultProvider":"anthropic","ownModelId":null,"effectiveModelId":"{{Haiku}}","source":1}]
        """;

    private static readonly string Models = $$"""
        [{"id":"{{Haiku}}","provider":"Anthropic","providerModelId":"claude-haiku-5-5","label":"Haiku","priceValidFrom":"2026-10-01T00:00:00Z","inputPerMillion":1,"outputPerMillion":5},
         {"id":"{{Guid.NewGuid()}}","provider":"Anthropic","providerModelId":"claude-opus-5-5","label":"Opus","priceValidFrom":null,"inputPerMillion":null,"outputPerMillion":null}]
        """;

    private FakeServer Api(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        FakeServer server = _session.Server(respond);
        PricelyApiClient api = _session.Api(server);
        Services.AddSingleton(api);
        Services.AddSingleton(new ReviewQueue(api));
        Services.AddSingleton<TimeProvider>(_session.Clock);

        return server;
    }
}
