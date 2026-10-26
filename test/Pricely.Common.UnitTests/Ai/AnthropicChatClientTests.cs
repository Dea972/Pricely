using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Infrastructure.Ai.Anthropic;

namespace Pricely.Common.UnitTests.Ai;

public sealed class AnthropicChatClientTests
{
    private const string ToolUseResponse = """
        {
          "id": "msg_1", "model": "claude-sonnet-5-5", "stop_reason": "tool_use",
          "content": [
            { "type": "text", "text": "Je cherche." },
            { "type": "tool_use", "id": "toolu_1", "name": "search_products", "input": { "text": "ciment", "limit": 5 } }
          ],
          "usage": { "input_tokens": 120, "output_tokens": 30, "cache_read_input_tokens": 10, "cache_creation_input_tokens": 4 }
        }
        """;

    private static readonly string[] ToolResult = ["p1"];

    [Fact]
    public async Task Request_ShouldCarryTheSystemPrompt_TheTools_AndTheToolRoundTrip()
    {
        using var handler = new RecordingHandler(ToolUseResponse);
        using HttpClient http = Http(handler);
        using var client = new AnthropicChatClient(http, Options.Create(new AnthropicOptions()), "claude-sonnet-5-5");
        AIFunction tool = AIFunctionFactory.Create((string text) => text, "search_products", "Recherche");

        await client.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, "Tu es l'assistant."),
                new ChatMessage(ChatRole.User, "Ciment ?"),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("toolu_0", "search_products", new Dictionary<string, object?> { ["text"] = "ciment" })]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("toolu_0", ToolResult)])
            ],
            new ChatOptions { Tools = [tool] });

        JsonNode body = JsonNode.Parse(handler.LastBody!)!;
        body["system"]!.GetValue<string>().Should().Be("Tu es l'assistant.");
        body["tools"]![0]!["name"]!.GetValue<string>().Should().Be("search_products");
        body["tools"]![0]!["input_schema"]!["properties"]!["text"].Should().NotBeNull();

        JsonArray messages = body["messages"]!.AsArray();
        messages.Select(m => m!["role"]!.GetValue<string>()).Should().Equal("user", "assistant", "user");
        messages[1]!["content"]![0]!["type"]!.GetValue<string>().Should().Be("tool_use");
        messages[1]!["content"]![0]!["input"]!["text"]!.GetValue<string>().Should().Be("ciment");
        messages[2]!["content"]![0]!["type"]!.GetValue<string>().Should().Be("tool_result");
        messages[2]!["content"]![0]!["tool_use_id"]!.GetValue<string>().Should().Be("toolu_0");
        messages[2]!["content"]![0]!["content"]!.GetValue<string>().Should().Be("[\"p1\"]");
    }

    [Fact]
    public async Task Response_ShouldExposeTheToolCalls_TheFinishReason_AndTheUsage()
    {
        using var handler = new RecordingHandler(ToolUseResponse);
        using HttpClient http = Http(handler);
        using var client = new AnthropicChatClient(http, Options.Create(new AnthropicOptions()), "claude-sonnet-5-5");

        ChatResponse response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Ciment ?")]);

        response.FinishReason.Should().Be(ChatFinishReason.ToolCalls);
        response.Text.Should().Be("Je cherche.");
        FunctionCallContent call = response.Messages.Single().Contents.OfType<FunctionCallContent>().Single();
        call.CallId.Should().Be("toolu_1");
        call.Name.Should().Be("search_products");
        ((JsonElement)call.Arguments!["limit"]!).GetInt32().Should().Be(5);
        response.Usage!.InputTokenCount.Should().Be(120);
        response.Usage.CachedInputTokenCount.Should().Be(10);
        response.Usage.AdditionalCounts![LlmChatUsage.CacheCreationInputTokens].Should().Be(4);
    }

    [Fact]
    public async Task ChatClientOfTheGateway_ShouldJournalEveryCall()
    {
        var eventBus = new RecordingEventBus();
        using var handler = new RecordingHandler(ToolUseResponse);
        using HttpClient http = Http(handler);
        var service = new AnthropicLlmService(
            http,
            Options.Create(new AnthropicOptions()),
            eventBus,
            new FixedDateTimeProvider());
        var feature = new AiFeature("agents.test", "Test", "Agents", AiProviderKeys.Anthropic);

        using IChatClient client = new LlmClient(service, feature, new LlmTarget(AiProviderKeys.Anthropic)).AsChatClient("conversation-1", "user-1");
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Ciment ?")]);

        LlmCalledIntegrationEvent called = eventBus.Published.Should().ContainSingle().Which.Should().BeOfType<LlmCalledIntegrationEvent>().Subject;
        called.SessionId.Should().Be("conversation-1");
        called.UserId.Should().Be("user-1");
        called.FeatureCode.Should().Be("agents.test");
        called.ModelName.Should().Be("claude-sonnet-5-5");
        called.InputTokens.Should().Be(120);
        called.InputCacheCreationTokens.Should().Be(4);
        called.IsSuccess.Should().BeTrue();
    }

    private static HttpClient Http(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.anthropic.test/") };

    private sealed class RecordingHandler(string responseBody) : HttpMessageHandler
    {
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
