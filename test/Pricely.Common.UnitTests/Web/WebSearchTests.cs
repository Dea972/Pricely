using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Ai.Anthropic;
using Pricely.Common.Infrastructure.Web.Search;
using Pricely.Common.UnitTests.Ai;

namespace Pricely.Common.UnitTests.Web;

public sealed class WebSearchTests
{
    // A paused turn (a search done, the model not finished), then the end of the turn with a citation.
    private const string PausedTurn = """
        {"model":"claude-test","stop_reason":"pause_turn",
         "content":[{"type":"server_tool_use","id":"srvtoolu_1","name":"web_search","input":{"query":"ciment 35 kg prix"}},
          {"type":"web_search_tool_result","tool_use_id":"srvtoolu_1","content":[
           {"type":"web_search_result","url":"https://www.chausson.fr/materiaux/ciment-p-199281-1","title":"Ciment Le Classic 32,5R","encrypted_content":"x","page_age":"October 20, 2026"},
           {"type":"web_search_result","url":"https://www.e.leclerc/fp/0192995410312","title":"Ciment E.Leclerc","encrypted_content":"y"}]}],
         "usage":{"input_tokens":1000,"output_tokens":50,"server_tool_use":{"web_search_requests":1}}}
        """;

    private const string EndOfTurn = """
        {"model":"claude-test","stop_reason":"end_turn",
         "content":[{"type":"text","text":"Chausson vend le sac à 17,64 €.","citations":[
           {"type":"web_search_result_location","url":"https://www.chausson.fr/materiaux/ciment-p-199281-1","title":"Ciment","encrypted_index":"z","cited_text":"17,64 € dont 0,42 € éco-contribution"}]},
          {"type":"web_search_tool_result","tool_use_id":"srvtoolu_2","content":[
           {"type":"web_search_result","url":"https://www.chausson.fr/materiaux/ciment-p-199281-1","title":"Doublon","encrypted_content":"x"}]}],
         "usage":{"input_tokens":1500,"output_tokens":120,"server_tool_use":{"web_search_requests":1}}}
        """;

    [Fact]
    public async Task Anthropic_ShouldListThePages_WithTheTextTheModelCited_AcrossAPausedTurn()
    {
        using var handler = new ScriptedHandler(PausedTurn, EndOfTurn);
        using HttpClient http = Http(handler);
        var eventBus = new RecordingEventBus();

        WebSearchResults results = await Anthropic(http, eventBus).SearchAsync(new WebSearchRequest("ciment 35 kg"), "session-1", CancellationToken.None);

        results.Provider.Should().Be(WebSearchProviders.Anthropic);
        results.Searches.Should().Be(2);
        results.Results.Should().BeEquivalentTo(
            [
                new WebSearchResult("Ciment Le Classic 32,5R", new Uri("https://www.chausson.fr/materiaux/ciment-p-199281-1"), "17,64 € dont 0,42 € éco-contribution", "October 20, 2026"),
                new WebSearchResult("Ciment E.Leclerc", new Uri("https://www.e.leclerc/fp/0192995410312"), null, null)
            ],
            options => options.WithStrictOrdering());

        handler.Bodies.Should().HaveCount(2);
        handler.Bodies[0].Should().Contain("\"web_search_20260318\"").And.Contain("\"direct\"").And.Contain("\"country\":\"FR\"");
        handler.Bodies[1].Should().Contain("\"role\":\"assistant\"").And.Contain("\"encrypted_content\":\"x\"", "the paused message goes back unchanged");

        LlmCalledIntegrationEvent call = eventBus.Published.OfType<LlmCalledIntegrationEvent>().Should().ContainSingle().Subject;
        call.FeatureCode.Should().Be(WebSearchFeatures.Search.Code);
        call.InputTokens.Should().Be(2500);
        call.OutputTokens.Should().Be(170);
        call.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Anthropic_ShouldFail_WhenEverySearchFailed()
    {
        using var handler = new ScriptedHandler("""
            {"model":"claude-test","stop_reason":"end_turn","content":[
              {"type":"web_search_tool_result","tool_use_id":"s","content":{"type":"web_search_tool_result_error","error_code":"too_many_requests"}}],
             "usage":{"input_tokens":10,"output_tokens":5}}
            """);

        using HttpClient http = Http(handler);
        Func<Task> search = () => Anthropic(http, new RecordingEventBus()).SearchAsync(new WebSearchRequest("ciment"), "s", CancellationToken.None);

        (await search.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("too_many_requests");
    }

    [Fact]
    public async Task Brave_ShouldListThePages_WithoutTheirHighlighting()
    {
        using var handler = new ScriptedHandler("""
            {"web":{"results":[
              {"title":"Ciment <strong>35 kg</strong>","url":"https://www.chausson.fr/p/1","description":"Sac de <strong>ciment</strong> &amp; mortier","age":"2 days ago"},
              {"title":"Doublon","url":"https://www.chausson.fr/p/1"}]}}
            """);

        using HttpClient http = Http(handler);
        WebSearchResults results = await new BraveWebSearchProvider(http, Monitor(new WebSearchOptions { Brave = new BraveOptions { ApiKey = "key" } }))
            .SearchAsync(new WebSearchRequest("ciment 35 kg") { MaxResults = 5 }, "s", CancellationToken.None);

        results.Searches.Should().Be(1);
        results.Results.Should().ContainSingle().Which.Should().Be(
            new WebSearchResult("Ciment 35 kg", new Uri("https://www.chausson.fr/p/1"), "Sac de ciment & mortier", "2 days ago"));
        handler.Requests.Should().ContainSingle().Which.Should().Contain("q=ciment%2035%20kg").And.Contain("count=5");
    }

    [Fact]
    public async Task Searcher_ShouldJournalTheSearch_AndStopAtTheDailyLimit()
    {
        var eventBus = new RecordingEventBus();
        using var handler = new ScriptedHandler(EndOfTurn, EndOfTurn);
        using HttpClient http = Http(handler);
        using ServiceProvider services = Services(http, eventBus, new WebSearchOptions { MaxSearchesPerDay = 1 });
        IWebSearcher searcher = services.GetRequiredService<IWebSearcher>();

        Result<WebSearchResults> first = await searcher.SearchAsync(new WebSearchRequest("ciment") { SessionId = "conversation-1" });
        Result<WebSearchResults> second = await searcher.SearchAsync(new WebSearchRequest("ciment"));

        first.IsSuccess.Should().BeTrue();
        WebSearchPerformedIntegrationEvent journal = eventBus.Published.OfType<WebSearchPerformedIntegrationEvent>().Should().ContainSingle().Subject;
        journal.Should().BeEquivalentTo(new { Provider = "Anthropic", SessionId = "conversation-1", Searches = 1, Results = 1, IsSuccess = true });
        second.Error.Code.Should().Be("WebSearch.DailyLimitReached");
    }

    private static AnthropicWebSearchProvider Anthropic(HttpClient http, RecordingEventBus eventBus) =>
        new(
            http,
            Options.Create(new AnthropicOptions { DefaultModel = "claude-test" }),
            Monitor(new WebSearchOptions()),
            new FixedTargetResolver(null),
            eventBus,
            new FixedDateTimeProvider());

    private static ServiceProvider Services(HttpClient http, RecordingEventBus eventBus, WebSearchOptions options)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptionsMonitor<WebSearchOptions>>(Monitor(options));
        services.AddSingleton(new WebSearchQuota());
        services.AddSingleton<Pricely.Common.Application.EventBus.IEventBus>(eventBus);
        services.AddSingleton<Pricely.Common.Application.Clock.IDateTimeProvider>(new FixedDateTimeProvider());
        services.AddKeyedSingleton<IWebSearchProvider>(WebSearchProviders.Anthropic, Anthropic(http, eventBus));
        services.AddScoped<IWebSearcher, WebSearcher>();

        return services.BuildServiceProvider();
    }

    private static StaticMonitor Monitor(WebSearchOptions options) => new(options);

    private static HttpClient Http(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.test/") };

    private sealed class StaticMonitor(WebSearchOptions value) : IOptionsMonitor<WebSearchOptions>
    {
        public WebSearchOptions CurrentValue => value;

        public WebSearchOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<WebSearchOptions, string?> listener) => null;
    }

    /// <summary>Answers each request with the next scripted body, and keeps what was asked.</summary>
    private sealed class ScriptedHandler(params string[] bodies) : HttpMessageHandler
    {
        private readonly Queue<string> _bodies = new(bodies);

        public List<string> Bodies { get; } = [];

        public List<string> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_bodies.Dequeue(), Encoding.UTF8, "application/json") };
        }
    }
}
