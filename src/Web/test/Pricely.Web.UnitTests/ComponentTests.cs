using System.Net;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pricely.Web.Api;
using Pricely.Web.Assistant;
using Pricely.Web.Components.Assistant;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.UnitTests;

public sealed class ComponentTests : BunitContext
{
    private static readonly Guid ProductId = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab6721c");
    private static readonly Guid ActionId = Guid.Parse("8d125e49-6386-4761-aa94-6638818018d8");

    private readonly TestSession _session = new TestSession().SignedIn(TimeSpan.FromHours(1));

    [Fact]
    public void ProposedAction_ShouldDescribeTheCollection_AndRunOnlyAfterValidation()
    {
        FakeServer api = Api(request => request.RequestUri!.AbsolutePath switch
        {
            var path when path.EndsWith("/collection-plan", StringComparison.Ordinal) => Responses.Json(CollectionPlan),
            var path when path.EndsWith("/approve", StringComparison.Ordinal) => Responses.Json(DecidedAction("Executed", """{"collectedCount":4,"proposedCount":1,"autoConfirmedCount":3,"observedCount":4,"ignoredCount":0,"anomalyCount":1}""")),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });

        IRenderedComponent<ProposedActionCard> card = Render<ProposedActionCard>(parameters => parameters.Add(p => p.Action, PendingCollection()));

        card.WaitForAssertion(() => card.Markup.Should().Contain("Ciment CEM II").And.Contain("Comptoir du Bâtiment, Négoce Atlantique"));
        api.Requests.Should().NotContain(r => r.Method == HttpMethod.Post, "nothing runs before « Valider »");

        card.Find("button.btn-primary").Click();

        card.WaitForAssertion(() => card.Markup.Should().Contain("Validée par toi").And.Contain("4 offre(s) collectée(s) · 1 à valider"));
        api.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post)
            .Which.RequestUri!.AbsolutePath.Should().Be($"/api/agents/actions/{ActionId}/approve");
        card.FindAll("button").Should().BeEmpty("a decided action keeps its trace, without buttons");
    }

    [Fact]
    public void ProposedAction_WithoutTheScope_ShouldSaySo()
    {
        Api(request => request.RequestUri!.AbsolutePath.EndsWith("/approve", StringComparison.Ordinal)
            ? Responses.Problem(HttpStatusCode.Forbidden, "Actions.MissingScope", "Approving this action requires the 'pricely.write' scope.")
            : Responses.Json(CollectionPlan));

        IRenderedComponent<ProposedActionCard> card = Render<ProposedActionCard>(parameters => parameters.Add(p => p.Action, PendingCollection()));
        card.Find("button.btn-primary").Click();

        card.WaitForAssertion(() => card.Markup.Should().Contain("Il te manque le droit de valider cette action (scope pricely.write)."));
        card.FindAll("button").Should().HaveCount(2, "the action is still pending");
    }

    [Fact]
    public void ARejectedAction_ShouldSayThatNothingChanged()
    {
        Api(request => request.RequestUri!.AbsolutePath.EndsWith("/reject", StringComparison.Ordinal)
            ? Responses.Json(DecidedAction("Rejected", "null"))
            : Responses.Json(CollectionPlan));

        IRenderedComponent<ProposedActionCard> card = Render<ProposedActionCard>(parameters => parameters.Add(p => p.Action, PendingCollection()));
        card.Find("button.btn-secondary").Click();

        card.WaitForAssertion(() => card.Markup.Should().Contain("Refusée").And.Contain("Rien n'a été modifié."));
    }

    [Fact]
    public void ComparePrices_ShouldShowTheComparison_WithTheBestAndTheSuspectPrices()
    {
        Api(_ => Responses.Json($$"""{"id":"{{ProductId}}","name":"Ciment CEM II","brand":"CR","gtin":"03456789012344","manufacturerReference":null,"productCategoryId":"{{Guid.NewGuid()}}"}"""));
        var output = new ToolOutput(
            "c1",
            ToolCatalog.ComparePrices,
            Json("""
                [
                  {"offerId":"00000000-0000-0000-0000-000000000001","merchantName":"Comptoir du Bâtiment","productPage":"https://comptoir.test/p","amount":9.4,"currency":"EUR","availability":"InStock","observedOnUtc":"2026-10-26T07:00:00Z","isFlaggedAsAnomaly":false},
                  {"offerId":"00000000-0000-0000-0000-000000000002","merchantName":"BricoPlus","productPage":"javascript:alert(1)","amount":94,"currency":"EUR","availability":"InStock","observedOnUtc":"2026-10-26T08:00:00Z","isFlaggedAsAnomaly":true}
                ]
                """),
            Json($$"""{"productId":"{{ProductId}}"}"""));

        IRenderedComponent<ToolResultView> view = Render<ToolResultView>(parameters => parameters.Add(p => p.Output, output));

        view.WaitForAssertion(() => view.Markup.Should().Contain("Ciment CEM II"));
        view.FindAll("tbody tr").Should().HaveCount(2);
        view.Find("tr.best").TextContent.Should().Contain("Comptoir du Bâtiment").And.Contain("Moins cher").And.Contain("9,40 €");
        view.Find("tr.flagged").TextContent.Should().Contain("Prix suspect").And.Contain("94,00 €");
        view.Find("tr.flagged").QuerySelector("a").Should().BeNull("a javascript: address is never a link");
        view.Find($"a[href='/products/{ProductId}']").TextContent.Should().Be("Ouvrir la fiche");
    }

    [Fact]
    public void ASuspectPrice_ShouldNeverBeCalledTheCheapest()
    {
        Api(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var output = new ToolOutput(
            "c1",
            ToolCatalog.ComparePrices,
            Json("""[{"offerId":"00000000-0000-0000-0000-000000000003","merchantName":"Lot de 10","productPage":"https://lot.test/p","amount":0.94,"currency":"EUR","availability":"InStock","observedOnUtc":"2026-10-26T08:00:00Z","isFlaggedAsAnomaly":true}]"""),
            null);

        IRenderedComponent<ToolResultView> view = Render<ToolResultView>(parameters => parameters.Add(p => p.Output, output));

        view.Markup.Should().Contain("Prix suspect").And.NotContain("Moins cher");
    }

    [Fact]
    public void AToolResultOfAnotherShape_ShouldShowNothing()
    {
        Api(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var output = new ToolOutput("c1", ToolCatalog.ComparePrices, Json("\"Non exécuté : le tour s'est arrêté avant.\""), null);

        IRenderedComponent<ToolResultView> view = Render<ToolResultView>(parameters => parameters.Add(p => p.Output, output));

        view.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void WebResults_ShouldBeMarkedUnverified_WithTheirSourceAndKnownMerchant()
    {
        Api(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var search = new ToolOutput("c1", ToolCatalog.SearchWeb, Json("""
            {"provider":"Anthropic","notice":"Prix non vérifiés.","results":[
             {"title":"Ciment 35 kg","address":"https://www.comptoir.test/p/1","snippet":"9,90 €","pageAge":null,
              "merchant":{"id":"8d125e49-6386-4761-aa94-6638818018d8","name":"Comptoir du Bâtiment","isActive":true}}]}
            """), null);
        var page = new ToolOutput("c2", ToolCatalog.ReadWebPage, Json("""
            {"address":"https://www.comptoir.test/p/1","title":"Ciment 35 kg","isTruncated":false,"merchant":null,"notice":"Prix non vérifiés.",
             "products":[{"name":"Ciment 35 kg","gtin":"3760000100014","brand":"Cimenterie","sku":null,
              "offers":[{"price":9.9,"currency":"EUR","availability":"InStock","seller":null,"isAggregate":false}]}]}
            """), null);

        IRenderedComponent<ToolResultView> searchView = Render<ToolResultView>(parameters => parameters.Add(p => p.Output, search));
        IRenderedComponent<ToolResultView> pageView = Render<ToolResultView>(parameters => parameters.Add(p => p.Output, page));

        searchView.Markup.Should().Contain("Non vérifié").And.Contain("https://www.comptoir.test/p/1").And.Contain("Comptoir du Bâtiment");
        pageView.Markup.Should().Contain("Non vérifié").And.Contain("3760000100014").And.Contain("9,90");
    }

    [Fact]
    public void Batch_ShouldDescribeEachAction_ApproveAllAtOnce_AndKeepEachOutcome()
    {
        var batchId = Guid.NewGuid();
        List<ProposedAction> actions = WebBatch(batchId);
        FakeServer api = Api(request => request.RequestUri!.AbsolutePath == $"/api/agents/batches/{batchId}/approve"
            ? Responses.Json($"[{BatchDto(actions[0], "Executed", ProductResult)},{BatchDto(actions[1], "Executed", MerchantResult)},{BatchDto(actions[2], "Failed", "null", "The price 9.4 does not appear on the page")}]")
            : Responses.Json("[]"));

        IRenderedComponent<ProposedBatchCard> card = Render<ProposedBatchCard>(parameters => parameters.Add(p => p.Actions, actions));

        card.Markup.Should().Contain("Lot proposé").And.Contain("3 action(s)")
            .And.Contain("Ciment 35 kg · Vicat · GTIN 3760000100014 · Gros œuvre › Ciment")
            .And.Contain("Chausson · www.chausson.test")
            .And.Contain("9,40").And.Contain("www.chausson.test");
        api.Requests.Should().NotContain(r => r.Method == HttpMethod.Post, "nothing runs before « Valider le lot »");

        card.Find("button.btn-primary").Click();

        card.WaitForAssertion(() => card.Markup.Should()
            .Contain("Ajouté au catalogue (typologie créée : Ciment)")
            .And.Contain("Enseigne déjà suivie")
            .And.Contain("The price 9.4 does not appear on the page")
            .And.Contain("2 exécutée(s) · 1 en échec"));
        api.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post);
        card.FindAll("button").Should().BeEmpty("a decided batch keeps its trace, without buttons");
    }

    [Fact]
    public void Batch_ShouldLetTheUserRemoveOneAction_BeforeDecidingTheRest()
    {
        List<ProposedAction> actions = WebBatch(Guid.NewGuid());
        FakeServer api = Api(request => request.RequestUri!.AbsolutePath.EndsWith("/reject", StringComparison.Ordinal)
            ? Responses.Json(BatchDto(actions[1], "Rejected", "null"))
            : Responses.Json("[]"));

        IRenderedComponent<ProposedBatchCard> card = Render<ProposedBatchCard>(parameters => parameters.Add(p => p.Actions, actions));
        card.FindAll("button").Where(b => b.TextContent.Contains("Retirer", StringComparison.Ordinal)).ElementAt(1).Click();

        card.WaitForAssertion(() => actions[1].Status.Should().Be("Rejected"));
        api.Requests.Should().ContainSingle(r => r.Method == HttpMethod.Post)
            .Which.RequestUri!.AbsolutePath.Should().Be($"/api/agents/actions/{actions[1].ActionId}/reject");
        card.Markup.Should().Contain("Valider le lot (2)");
    }

    [Fact]
    public void Answer_ShouldShowTheActionsOfABatchTogether_AndWaitForItsEndToDecide()
    {
        Api(_ => Responses.Json("[]"));
        var view = new ConversationView();
        AgentEntry answer = view.Send("Ajoute ce ciment");
        var batchId = Guid.NewGuid();
        foreach (ProposedAction action in WebBatch(batchId).Take(2))
        {
            view.Apply(new ActionProposedEvent(action.ActionId, "c-" + action.ActionId, action.ToolName, action.Arguments, "pricely.write") { BatchId = batchId });
        }

        IRenderedComponent<ChatAnswer> running = Render<ChatAnswer>(parameters => parameters.Add(p => p.Answer, answer));

        running.FindAll("article[aria-label^=\"Lot d\"]").Should().ContainSingle();
        running.FindAll("article[aria-label^=\"Action proposée\"]").Should().BeEmpty();
        running.Markup.Should().Contain("L'assistant prépare encore le lot");
        running.Find("button.btn-primary").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void TheFirstActionOfABatch_ShouldNotBeDecidableAlone_WhileTheAnswerRuns()
    {
        Api(_ => Responses.Json("[]"));
        var view = new ConversationView();
        AgentEntry answer = view.Send("Ajoute ce ciment");
        ProposedAction first = WebBatch(Guid.NewGuid())[0];
        view.Apply(new ActionProposedEvent(first.ActionId, "c1", first.ToolName, first.Arguments, "pricely.write") { BatchId = first.BatchId });

        IRenderedComponent<ChatAnswer> running = Render<ChatAnswer>(parameters => parameters.Add(p => p.Answer, answer));

        running.FindAll("article[aria-label^=\"Lot d\"]").Should().ContainSingle();
        running.FindAll("button").Should().OnlyContain(b => b.HasAttribute("disabled"));

        view.Apply(new TurnDone(Guid.NewGuid()));
        IRenderedComponent<ChatAnswer> done = Render<ChatAnswer>(parameters => parameters.Add(p => p.Answer, answer));

        done.FindAll("article[aria-label^=\"Action proposée\"]").Should().ContainSingle("an action proposed alone keeps its usual card");
    }

    [Fact]
    public void ASingleWebOffer_ShouldSayWhatHappened()
    {
        var offer = new ProposedAction(ActionId, ToolCatalog.AddWebOffer, Json("""{"pageAddress":"https://www.chausson.test/p/1","price":9.4}"""), "pricely.write");
        offer.Update(new PendingActionDto
        {
            Id = ActionId,
            Status = ApiValues.Executed,
            Result = Json($$"""{"productId":"{{ProductId}}","productName":"Ciment","merchantId":"{{ActionId}}","merchantName":"Chausson","pageAddress":"https://www.chausson.test/p/1","price":9.4,"currency":"EUR","outcome":"NewOfferProposed","isFlaggedAsAnomaly":true,"addedToProfile":true}""")
        });
        Api(_ => Responses.Json("[]"));

        IRenderedComponent<ProposedActionCard> card = Render<ProposedActionCard>(parameters => parameters.Add(p => p.Action, offer));

        card.Markup.Should().Contain("Offre à valider").And.Contain("prix suspect").And.Contain("enseigne ajoutée au profil de la typologie");
    }

    [Fact]
    public void ARunningAnswer_ShouldSayThatTheAssistantWorks()
    {
        Api(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var view = new ConversationView();
        AgentEntry answer = view.Send("Bonjour");
        view.Apply(new ToolCalled("c1", "search_products", true, null));

        IRenderedComponent<ChatAnswer> cut = Render<ChatAnswer>(parameters => parameters.Add(p => p.Answer, answer));

        cut.Find(".typing").TextContent.Should().Be("L'assistant travaille…");
        cut.Find(".chip").TextContent.Should().Contain("search_products").And.Contain("en cours");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _session.Dispose();
        }

        base.Dispose(disposing);
    }

    private const string CollectionPlan = """
        {"product":{"productId":"01a176e6-c58f-73c5-a435-1690cab6721c","name":"Ciment CEM II","brand":"CR","gtin":null,"manufacturerReference":null},
         "merchants":[{"merchantId":"00000000-0000-0000-0000-000000000002","name":"Négoce Atlantique","website":"https://negoce.test/","priority":2},
                      {"merchantId":"00000000-0000-0000-0000-000000000001","name":"Comptoir du Bâtiment","website":"https://comptoir.test/","priority":1}],
         "instructions":""}
        """;

    private FakeServer Api(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        FakeServer server = _session.Server(respond);
        PricelyApiClient api = _session.Api(server);
        Services.AddSingleton(api);
        Services.AddSingleton(new ProductLookup(api));
        Services.AddSingleton(new ReviewQueue(api));
        Services.AddSingleton<TimeProvider>(_session.Clock);

        return server;
    }

    private static ProposedAction PendingCollection() =>
        new(ActionId, ToolCatalog.CollectProductOffers, Json($$"""{"productId":"{{ProductId}}"}"""), "pricely.write");

    private const string ProductResult = """{"productId":"01a176e6-c58f-73c5-a435-1690cab6721c","name":"Ciment 35 kg","brand":"Vicat","gtin":"3760000100014","productCategoryId":"8d125e49-6386-4761-aa94-6638818018d8","isNew":true,"createdCategories":["Ciment"]}""";

    private const string MerchantResult = """{"merchantId":"8d125e49-6386-4761-aa94-6638818018d8","name":"Chausson","website":"https://www.chausson.test/","isActive":true,"isNew":false}""";

    /// <summary>A batch built from a web research: a product, its merchant, then the offer.</summary>
    private static List<ProposedAction> WebBatch(Guid batchId) =>
    [
        new(Guid.NewGuid(), ToolCatalog.FindOrAddProduct, Json("""{"name":"Ciment 35 kg","brand":"Vicat","gtin":"3760000100014","categoryPath":["Gros œuvre","Ciment"]}"""), "pricely.write") { BatchId = batchId },
        new(Guid.NewGuid(), ToolCatalog.FindOrAddMerchant, Json("""{"name":"Chausson","website":"https://www.chausson.test/p/ciment"}"""), "pricely.write") { BatchId = batchId },
        new(Guid.NewGuid(), ToolCatalog.AddWebOffer, Json("""{"pageAddress":"https://www.chausson.test/p/ciment","price":9.4,"productGtin":"3760000100014"}"""), "pricely.write") { BatchId = batchId }
    ];

    private static string BatchDto(ProposedAction action, string status, string result, string? error = null) =>
        $$"""{"id":"{{action.ActionId}}","conversationId":"{{Guid.NewGuid()}}","toolName":"{{action.ToolName}}","arguments":{},"requiredScope":"pricely.write","status":"{{status}}","proposedOnUtc":"2026-10-27T08:00:00Z","decidedOnUtc":"2026-10-27T08:01:00Z","result":{{result}},"error":{{(error is null ? "null" : $"\"{error}\"")}},"batchId":"{{action.BatchId}}"}""";

    private static string DecidedAction(string status, string result) =>
        $$"""{"id":"{{ActionId}}","conversationId":"{{Guid.NewGuid()}}","toolName":"collect_product_offers","arguments":{},"requiredScope":"pricely.write","status":"{{status}}","proposedOnUtc":"2026-10-26T08:00:00Z","decidedOnUtc":"2026-10-26T08:01:00Z","result":{{result}},"error":null}""";

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }
}
