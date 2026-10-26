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

    private const string StreamedResponse = """
        event: message_start
        data: {"type":"message_start","message":{"id":"msg_2","model":"claude-sonnet-5-5","usage":{"input_tokens":200,"cache_read_input_tokens":20,"cache_creation_input_tokens":0,"output_tokens":1}}}

        event: content_block_start
        data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Je "}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"cherche."}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":0}

        event: content_block_start
        data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"toolu_2","name":"search_products","input":{}}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":"{\"text\": \"cim"}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":"ent\", \"limit\": 5}"}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":1}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"tool_use"},"usage":{"output_tokens":42}}

        event: message_stop
        data: {"type":"message_stop"}


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

    [Fact]
    public async Task Streaming_ShouldYieldTheTextAsItComes_ThenTheToolCall_ThenTheUsage()
    {
        using var handler = new RecordingHandler(StreamedResponse, "text/event-stream");
        using HttpClient http = Http(handler);
        using var client = new AnthropicChatClient(http, Options.Create(new AnthropicOptions()), "claude-sonnet-5-5");

        var updates = new List<ChatResponseUpdate>();
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Ciment ?")]))
        {
            updates.Add(update);
        }

        JsonNode.Parse(handler.LastBody!)!["stream"]!.GetValue<bool>().Should().BeTrue();
        updates.Where(u => u.Text.Length > 0).Select(u => u.Text).Should().Equal("Je ", "cherche.");

        var response = updates.ToChatResponse();
        response.Text.Should().Be("Je cherche.");
        response.FinishReason.Should().Be(ChatFinishReason.ToolCalls);
        FunctionCallContent call = response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Single();
        call.Name.Should().Be("search_products");
        ((JsonElement)call.Arguments!["text"]!).GetString().Should().Be("ciment");
        ((JsonElement)call.Arguments["limit"]!).GetInt32().Should().Be(5);
        response.Usage!.InputTokenCount.Should().Be(200);
        response.Usage.OutputTokenCount.Should().Be(42);
        response.Usage.CachedInputTokenCount.Should().Be(20);
    }

    [Fact]
    public async Task Streaming_ShouldFail_OnAnErrorEvent()
    {
        using var handler = new RecordingHandler("""
            event: error
            data: {"type":"error","error":{"type":"overloaded_error","message":"Overloaded"}}


            """, "text/event-stream");
        using HttpClient http = Http(handler);
        using var client = new AnthropicChatClient(http, Options.Create(new AnthropicOptions()), "claude-sonnet-5-5");

        Func<Task> act = async () =>
        {
            await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "Ciment ?")]))
            {
                update.Should().BeNull("no update is expected");
            }
        };

        (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should().Contain("overloaded_error");
    }

    private static HttpClient Http(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.anthropic.test/") };

    private sealed class RecordingHandler(string responseBody, string mediaType = "application/json") : HttpMessageHandler
    {
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, mediaType)
            };
        }
    }
}
