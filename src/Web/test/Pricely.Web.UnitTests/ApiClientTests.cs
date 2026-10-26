using System.Net;
using FluentAssertions;
using Pricely.Web.Api;
using Pricely.Web.Authentication;

namespace Pricely.Web.UnitTests;

public sealed class ApiClientTests
{
    private static readonly Guid ProductId = Guid.Parse("01a176e6-c58f-73c5-a435-1690cab6721c");

    [Fact]
    public async Task Calls_ShouldCarryTheUsersToken_UnderApi()
    {
        using TestSession session = new TestSession().SignedIn(TimeSpan.FromMinutes(10));
        FakeServer api = session.Server(_ => Responses.Json("""{"id":"01a176e6-c58f-73c5-a435-1690cab6721c","name":"Ciment","brand":"CR","gtin":null,"manufacturerReference":"CR-35","productCategoryId":"01a176e6-48d1-720b-ba93-9d9b1d361554"}"""));

        ProductDto product = await session.Api(api).GetProductAsync(ProductId, CancellationToken.None);

        product.Name.Should().Be("Ciment");
        HttpRequestMessage request = api.Requests.Should().ContainSingle().Subject;
        request.RequestUri!.AbsolutePath.Should().Be($"/api/catalog/products/{ProductId}");
        request.Headers.Authorization!.ToString().Should().Be("Bearer access-1");
    }

    [Fact]
    public async Task ARefusedCall_ShouldCarryTheErrorCodeOfTheModule()
    {
        using TestSession session = new TestSession().SignedIn(TimeSpan.FromMinutes(10));
        FakeServer api = session.Server(_ => Responses.Problem(HttpStatusCode.Conflict, "Actions.AlreadyDecided", "The action has already been approved or rejected."));

        Func<Task> act = () => session.Api(api).ApproveActionAsync(Guid.NewGuid(), CancellationToken.None);

        PricelyApiException refused = (await act.Should().ThrowAsync<PricelyApiException>()).Which;
        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        refused.Code.Should().Be("Actions.AlreadyDecided");
        refused.Message.Should().Be("The action has already been approved or rejected.");
    }

    [Fact]
    public async Task AnUnauthorizedCall_ShouldMeanAnExpiredSession()
    {
        using TestSession session = new TestSession().SignedIn(TimeSpan.FromMinutes(10));
        FakeServer api = session.Server(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        Func<Task> act = () => session.Api(api).GetCategoriesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<SessionExpiredException>();
    }

    [Fact]
    public async Task AMessage_ShouldStreamTheEventsOfTheTurn()
    {
        const string body = """
            event: conversation
            data: {"conversationId":"01a177f1-73d0-7e58-a195-f109bae9369f","title":"Bonjour"}

            event: tool_call
            data: {"callId":"c1","name":"compare_prices","isReadOnly":true,"arguments":{"productId":"01a176e6-c58f-73c5-a435-1690cab6721c"}}

            event: tool_result
            data: {"callId":"c1","name":"compare_prices","result":[],"error":null}

            event: something_new
            data: {}

            event: text
            data: {"text":"Voici."}

            event: done
            data: {"conversationId":"01a177f1-73d0-7e58-a195-f109bae9369f"}


            """;
        using TestSession session = new TestSession().SignedIn(TimeSpan.FromMinutes(10));
        FakeServer api = session.Server(_ => Responses.Events(body));

        var events = new List<AgentStreamEvent>();
        await foreach (AgentStreamEvent agentEvent in session.Agent(api).SendMessageAsync("assistant", null, "Bonjour", CancellationToken.None))
        {
            events.Add(agentEvent);
        }

        events.Select(e => e.GetType()).Should().Equal(
            typeof(ConversationStarted), typeof(ToolCalled), typeof(ToolResulted), typeof(TextReceived), typeof(TurnDone));
        ((ToolCalled)events[1]).Name.Should().Be("compare_prices");
        ((TextReceived)events[3]).Text.Should().Be("Voici.");

        HttpRequestMessage request = api.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.AbsolutePath.Should().Be("/api/agents/assistant/messages");
        api.Bodies[0].Should().Be("""{"conversationId":null,"message":"Bonjour"}""");
    }

    [Fact]
    public async Task ARefusedMessage_ShouldFailBeforeAnyEvent()
    {
        using TestSession session = new TestSession().SignedIn(TimeSpan.FromMinutes(10));
        FakeServer api = session.Server(_ => Responses.Problem(HttpStatusCode.NotFound, "Conversations.UnknownAgent", "The agent 'x' does not exist."));

        Func<Task> act = async () =>
        {
            await foreach (AgentStreamEvent agentEvent in session.Agent(api).SendMessageAsync("x", null, "Bonjour", CancellationToken.None))
            {
                agentEvent.Should().BeNull("no event is expected");
            }
        };

        (await act.Should().ThrowAsync<PricelyApiException>()).Which.Code.Should().Be("Conversations.UnknownAgent");
    }
}
