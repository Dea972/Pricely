using System.Net;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Web.Api;
using Pricely.Web.Components.Pages;

namespace Pricely.Web.UnitTests;

/// <summary>The screens that fill Pricely: the catalogue (typologies, products) and the merchants.</summary>
public sealed class AdminScreenTests : BunitContext
{
    private static readonly Guid Root = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67601");
    private static readonly Guid Cement = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67602");
    private static readonly Guid Merchant = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab67701");

    private readonly TestSession _session = new TestSession().SignedIn(TimeSpan.FromHours(1));

    [Fact]
    public void Catalog_ShouldListTheProductsOfATypology_AndAddOne()
    {
        FakeServer api = Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/catalog/categories") => Responses.Json(Categories),
            ("GET", "/api/catalog/products") => Responses.Json($$"""[{"id":"{{Guid.NewGuid()}}","name":"Ciment CEM II 35 kg","brand":"CR","gtin":"3760000100014","manufacturerReference":"CR-325R-35","productCategoryId":"{{Cement}}"}]"""),
            ("POST", "/api/catalog/products") => Responses.Json($"\"{Guid.NewGuid()}\""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<CatalogPage> page = Render<CatalogPage>(parameters => parameters.Add(p => p.CategoryId, Cement));

        page.WaitForAssertion(() => page.Markup.Should().Contain("Ciment CEM II 35 kg").And.Contain("Gros œuvre › Ciment"));
        api.Requests.Should().Contain(r => r.RequestUri!.Query.Contains($"productCategoryId={Cement}", StringComparison.Ordinal));

        page.Find("form[aria-labelledby='product-form'] input[required]").Change("Mortier colle C2TE 25 kg");
        page.FindAll("form[aria-labelledby='product-form'] input[required]")[1].Change("Fixalis");
        page.FindAll("form[aria-labelledby='product-form'] input")[3].Change("3760000100021");
        page.Find("form[aria-labelledby='product-form']").Submit();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Produit ajouté."));
        int post = api.Requests.FindIndex(r => r.Method == HttpMethod.Post);
        api.Bodies[post].Should().Be($$"""{"name":"Mortier colle C2TE 25 kg","brand":"Fixalis","gtin":"3760000100021","manufacturerReference":null,"productCategoryId":"{{Cement}}"}""");
    }

    [Fact]
    public void Catalog_ShouldShowTheErrorOfTheApi_AndKeepWhatWasTyped()
    {
        Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/catalog/categories") => Responses.Json(Categories),
            ("GET", _) => Responses.Json("[]"),
            ("POST", _) => Responses.Problem(HttpStatusCode.BadRequest, "Products.InvalidGtin", "The GTIN '123' is not valid."),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<CatalogPage> page = Render<CatalogPage>(parameters => parameters.Add(p => p.CategoryId, Cement));
        page.WaitForAssertion(() => page.Markup.Should().Contain("Aucun produit dans cette typologie"));

        page.Find("form[aria-labelledby='product-form'] input[required]").Change("Produit");
        page.FindAll("form[aria-labelledby='product-form'] input[required]")[1].Change("Marque");
        page.FindAll("form[aria-labelledby='product-form'] input")[3].Change("123");
        page.Find("form[aria-labelledby='product-form']").Submit();

        page.WaitForAssertion(() => page.Find(".alert-warn").TextContent.Should().Contain("The GTIN '123' is not valid."));
        page.FindAll("form[aria-labelledby='product-form'] input")[3].GetAttribute("value").Should().Be("123");
    }

    [Fact]
    public void Merchants_ShouldReactivateAnInactiveMerchant()
    {
        bool active = false;
        FakeServer api = Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/pricing/merchants") => Responses.Json($$"""[{"id":"{{Merchant}}","name":"Quincaillerie Martin","website":"https://www.quincaillerie-martin.example/","isActive":{{(active ? "true" : "false")}},"searchTemplate":null}]"""),
            ("POST", var path) when path.EndsWith("/activate", StringComparison.Ordinal) => Activate(ref active),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<MerchantsPage> page = Render<MerchantsPage>();
        page.WaitForAssertion(() => page.Markup.Should().Contain("○ Inactive"));

        page.FindAll("button").Single(b => b.TextContent == "Réactiver").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("● Active").And.Contain("Quincaillerie Martin est de nouveau interrogée."));
        api.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post)
            .Which.RequestUri!.AbsolutePath.Should().Be($"/api/pricing/merchants/{Merchant}/activate");
    }

    [Fact]
    public void Merchants_ShouldRegisterAMerchant_WithItsSearchPage()
    {
        FakeServer api = Api(request => request.Method == HttpMethod.Post
            ? Responses.Json($"\"{Guid.NewGuid()}\"")
            : Responses.Json("[]"));

        IRenderedComponent<MerchantsPage> page = Render<MerchantsPage>();
        page.WaitForAssertion(() => page.Markup.Should().Contain("Aucune enseigne"));

        page.FindAll("form[aria-labelledby='new-merchant'] input")[0].Change("BricoPlus");
        page.FindAll("form[aria-labelledby='new-merchant'] input")[1].Change("https://www.bricoplus.example/");
        page.FindAll("form[aria-labelledby='new-merchant'] input")[2].Change("https://www.bricoplus.example/s?k={query}");
        page.Find("form[aria-labelledby='new-merchant']").Submit();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Enseigne « BricoPlus » ajoutée."));
        api.Bodies[api.Requests.FindIndex(r => r.Method == HttpMethod.Post)]
            .Should().Be("""{"name":"BricoPlus","website":"https://www.bricoplus.example/","searchTemplate":"https://www.bricoplus.example/s?k={query}"}""");
    }

    [Fact]
    public void MerchantAssistant_ShouldProposeTheSetup_ThenCreateTheMerchant_AndAddItToTheProfiles()
    {
        var created = Guid.NewGuid();
        FakeServer api = Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/pricing/merchants") => Responses.Json("[]"),
            ("POST", "/api/pricing/merchants/suggestions") => Responses.Json($$"""
                {"name":"Comptoir du Bâtiment","website":"https://www.comptoir.test/","searchTemplate":"https://www.comptoir.test/recherche?q={query}",
                 "searchCandidates":["https://www.comptoir.test/recherche?q={query}"],
                 "searchCheck":{"query":"ciment","testedAddress":"https://www.comptoir.test/recherche?q=ciment","isReachable":true,"showsQuery":true,"textLength":4000},
                 "categories":[{"productCategoryId":"{{Cement}}","name":"Gros œuvre › Ciment","reason":"Vend du ciment en sac."}],
                 "rationale":"Négoce de matériaux.","isAiAnswer":true}
                """),
            ("POST", "/api/pricing/merchants") => Responses.Json($"\"{created}\""),
            ("POST", _) => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<MerchantsPage> page = Render<MerchantsPage>();
        page.Find("section[aria-labelledby='merchant-assistant'] input").Change("comptoir.test");
        page.Find("section[aria-labelledby='merchant-assistant'] form").Submit();

        page.WaitForAssertion(() => page.Markup.Should().Contain("✓ Recherche testée : « ciment » renvoie une page de résultats.")
            .And.Contain("Gros œuvre › Ciment").And.Contain("Négoce de matériaux."));

        page.FindAll("button").Single(b => b.TextContent == "Créer l'enseigne").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Enseigne « Comptoir du Bâtiment » créée et ajoutée à 1 profil(s) de collecte."));
        int profiles = api.Requests.FindIndex(r => r.RequestUri!.AbsolutePath == $"/api/pricing/merchants/{created}/profiles");
        api.Bodies[profiles].Should().Be($$"""{"productCategoryIds":["{{Cement}}"]}""");
        api.Bodies[api.Requests.FindIndex(r => r.RequestUri!.AbsolutePath == "/api/pricing/merchants" && r.Method == HttpMethod.Post)]
            .Should().Contain("\"searchTemplate\":\"https://www.comptoir.test/recherche?q={query}\"");
    }

    [Fact]
    public void ProductAssistant_ShouldFillTheForm_AndPointOutAnExistingProduct()
    {
        var existing = Guid.NewGuid();
        Api(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/api/catalog/categories") => Responses.Json(Categories),
            ("GET", _) => Responses.Json("[]"),
            ("POST", "/api/catalog/products/suggestions") => Responses.Json($$"""
                {"productPage":"https://fabricant.test/p","name":"Ciment CEM II — sac 35 kg","brand":"CR","gtin":"3760000100014","manufacturerReference":null,
                 "productCategoryId":"{{Cement}}","packaging":"sac 35 kg","rationale":"GTIN affiché.","existingProductId":"{{existing}}","existingProductName":"Ciment 35 kg","isAiAnswer":true}
                """),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<CatalogPage> page = Render<CatalogPage>(parameters => parameters.Add(p => p.CategoryId, Cement));
        page.WaitForAssertion(() => page.Find("#product-page"));
        page.Find("#product-page").Change("https://fabricant.test/p");
        page.FindAll("button").Single(b => b.TextContent == "Pré-remplir avec l'IA").Click();

        page.WaitForAssertion(() => page.Markup.Should().Contain("Conditionnement : sac 35 kg.").And.Contain($"href=\"/products/{existing}\""));
        page.FindAll("form[aria-labelledby='product-form'] input[required]")[0].GetAttribute("value").Should().Be("Ciment CEM II — sac 35 kg");
        page.FindAll("form[aria-labelledby='product-form'] input")[3].GetAttribute("value").Should().Be("3760000100014");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.Dispose();
        }

        base.Dispose(disposing);
    }

    private static readonly string Categories = $$"""
        [{"id":"{{Root}}","name":"Gros œuvre","parentId":null,"depth":1},{"id":"{{Cement}}","name":"Ciment","parentId":"{{Root}}","depth":2}]
        """;

    private static HttpResponseMessage Activate(ref bool active)
    {
        active = true;

        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private FakeServer Api(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        FakeServer server = _session.Server(respond);
        Services.AddSingleton(_session.Api(server));
        Services.AddSingleton<TimeProvider>(_session.Clock);

        return server;
    }
}
