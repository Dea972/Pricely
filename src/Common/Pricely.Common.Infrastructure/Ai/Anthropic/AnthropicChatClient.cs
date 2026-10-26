using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;

namespace Pricely.Common.Infrastructure.Ai.Anthropic;

/// <summary>
/// <see cref="IChatClient"/> over the Anthropic Messages API with tool use: <see cref="AIFunctionDeclaration"/>s become
/// <c>tools</c>, <c>tool_use</c> blocks become <see cref="FunctionCallContent"/> and <see cref="FunctionResultContent"/>
/// is sent back as <c>tool_result</c>. Tool invocation itself is left to <c>FunctionInvokingChatClient</c>.
/// Streaming reads the server-sent events of the API (<c>stream: true</c>): text as it is generated, each tool call once
/// its input is complete, the usage at the end.
/// </summary>
internal sealed class AnthropicChatClient(HttpClient httpClient, IOptions<AnthropicOptions> anthropicOptions, string model) : IChatClient
{
    // Compact JSON: tool results are sent back to the model and count as input tokens.
    private static readonly JsonSerializerOptions CompactOptions = new(AIJsonUtilities.DefaultOptions) { WriteIndented = false };

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        JsonObject body = BuildRequest(messages, options);

        using HttpResponseMessage httpResponse = await httpClient.PostAsJsonAsync(
            new Uri("v1/messages", UriKind.Relative),
            body,
            cancellationToken);

        await EnsureSuccessAsync(httpResponse, cancellationToken);

        using JsonDocument document = await JsonDocument.ParseAsync(
            await httpResponse.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        return ParseResponse(document.RootElement);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        JsonObject body = BuildRequest(messages, options);
        body["stream"] = true;

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("v1/messages", UriKind.Relative))
        {
            Content = JsonContent.Create(body)
        };

        using HttpResponseMessage httpResponse = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(httpResponse, cancellationToken);

        await using Stream stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        var state = new StreamState(model);

        await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            if (string.IsNullOrEmpty(item.Data))
            {
                continue;
            }

            using var document = JsonDocument.Parse(item.Data);
            if (state.Read(item.EventType, document.RootElement) is ChatResponseUpdate update)
            {
                yield return update;
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose()
    {
        // The HttpClient belongs to IHttpClientFactory.
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage httpResponse, CancellationToken cancellationToken)
    {
        if (httpResponse.IsSuccessStatusCode)
        {
            return;
        }

        string error = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        throw new HttpRequestException(
            $"Anthropic answered {(int)httpResponse.StatusCode}: {error}",
            inner: null,
            statusCode: httpResponse.StatusCode);
    }

    private JsonObject BuildRequest(IEnumerable<ChatMessage> messages, ChatOptions? chatOptions)
    {
        var system = new List<string>();
        if (!string.IsNullOrWhiteSpace(chatOptions?.Instructions))
        {
            system.Add(chatOptions.Instructions);
        }

        var turns = new JsonArray();
        string? currentRole = null;
        JsonArray? currentContent = null;

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatRole.System)
            {
                system.Add(message.Text);
                continue;
            }

            // Tool results are sent by the user side of the conversation; consecutive turns of a role are merged.
            string role = message.Role == ChatRole.Assistant ? "assistant" : "user";
            JsonArray blocks = ToBlocks(message);
            if (blocks.Count == 0)
            {
                continue;
            }

            if (role != currentRole || currentContent is null)
            {
                currentContent = [];
                turns.Add(new JsonObject { ["role"] = role, ["content"] = currentContent });
                currentRole = role;
            }

            foreach (JsonNode? block in blocks.ToList())
            {
                blocks.Remove(block);
                currentContent.Add(block);
            }
        }

        var body = new JsonObject
        {
            ["model"] = chatOptions?.ModelId ?? model,
            ["max_tokens"] = chatOptions?.MaxOutputTokens ?? anthropicOptions.Value.MaxTokens,
            ["messages"] = turns
        };

        if (system.Count > 0)
        {
            body["system"] = string.Join("\n\n", system);
        }

        if (chatOptions?.Temperature is float temperature)
        {
            body["temperature"] = temperature;
        }

        var tools = new JsonArray();
        foreach (AIFunctionDeclaration tool in chatOptions?.Tools?.OfType<AIFunctionDeclaration>() ?? [])
        {
            tools.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["input_schema"] = JsonNode.Parse(tool.JsonSchema.GetRawText())
            });
        }

        if (tools.Count > 0)
        {
            body["tools"] = tools;
        }

        return body;
    }

    private static JsonArray ToBlocks(ChatMessage message)
    {
        var blocks = new JsonArray();

        foreach (AIContent content in message.Contents)
        {
            switch (content)
            {
                case TextContent text when !string.IsNullOrEmpty(text.Text):
                    blocks.Add(new JsonObject { ["type"] = "text", ["text"] = text.Text });
                    break;
                case FunctionCallContent call:
                    blocks.Add(new JsonObject
                    {
                        ["type"] = "tool_use",
                        ["id"] = call.CallId,
                        ["name"] = call.Name,
                        ["input"] = JsonSerializer.SerializeToNode(call.Arguments ?? new Dictionary<string, object?>(), CompactOptions)
                    });
                    break;
                case FunctionResultContent result:
                    blocks.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = result.CallId,
                        ["content"] = result.Result switch
                        {
                            null => result.Exception?.Message ?? string.Empty,
                            string value => value,
                            JsonElement element => element.GetRawText(),
                            object value => JsonSerializer.Serialize(value, CompactOptions)
                        },
                        ["is_error"] = result.Exception is not null
                    });
                    break;
                default:
                    break;
            }
        }

        return blocks;
    }

    private ChatResponse ParseResponse(JsonElement root)
    {
        var contents = new List<AIContent>();

        if (root.TryGetProperty("content", out JsonElement blocks))
        {
            foreach (JsonElement block in blocks.EnumerateArray())
            {
                string? type = block.GetProperty("type").GetString();

                if (type == "text")
                {
                    contents.Add(new TextContent(block.GetProperty("text").GetString()));
                }
                else if (type == "tool_use")
                {
                    var arguments = block.GetProperty("input")
                        .EnumerateObject()
                        .ToDictionary(p => p.Name, p => (object?)p.Value.Clone());

                    contents.Add(new FunctionCallContent(
                        block.GetProperty("id").GetString()!,
                        block.GetProperty("name").GetString()!,
                        arguments));
                }
            }
        }

        string? stopReason = root.TryGetProperty("stop_reason", out JsonElement stop) ? stop.GetString() : null;
        string? modelId = root.TryGetProperty("model", out JsonElement modelElement) ? modelElement.GetString() : model;

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, contents))
        {
            ModelId = modelId,
            ResponseId = root.TryGetProperty("id", out JsonElement id) ? id.GetString() : null,
            FinishReason = ToFinishReason(stopReason),
            Usage = root.TryGetProperty("usage", out JsonElement usage) ? ParseUsage(usage) : null
        };
    }

    private static ChatFinishReason ToFinishReason(string? stopReason) =>
        stopReason switch
        {
            "tool_use" => ChatFinishReason.ToolCalls,
            "max_tokens" => ChatFinishReason.Length,
            _ => ChatFinishReason.Stop
        };

    private static long ReadCount(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;

    private static UsageDetails ParseUsage(JsonElement usage) =>
        Usage(ReadCount(usage, "input_tokens"), ReadCount(usage, "output_tokens"), ReadCount(usage, "cache_read_input_tokens"), ReadCount(usage, "cache_creation_input_tokens"));

    private static UsageDetails Usage(long input, long output, long cacheRead, long cacheCreation) =>
        new()
        {
            InputTokenCount = input,
            OutputTokenCount = output,
            TotalTokenCount = input + output,
            CachedInputTokenCount = cacheRead,
            AdditionalCounts = new AdditionalPropertiesDictionary<long>
            {
                [LlmChatUsage.CacheCreationInputTokens] = cacheCreation
            }
        };

    /// <summary>
    /// The events of one streamed message (<c>message_start</c>, <c>content_block_*</c>, <c>message_delta</c>): text
    /// deltas go out at once, a tool call once its JSON input is complete, the usage and the stop reason at the end.
    /// </summary>
    private sealed class StreamState(string model)
    {
        private readonly Dictionary<int, ToolCallInProgress> _toolCalls = [];
        private string? _messageId;
        private string? _modelId;
        private long _inputTokens;
        private long _cacheRead;
        private long _cacheCreation;

        public ChatResponseUpdate? Read(string eventType, JsonElement data) =>
            eventType switch
            {
                "message_start" => Start(data.GetProperty("message")),
                "content_block_start" => StartBlock(data),
                "content_block_delta" => Delta(data),
                "content_block_stop" => StopBlock(data),
                "message_delta" => End(data),
                "error" => throw new HttpRequestException($"Anthropic stream failed: {data.GetRawText()}"),
                _ => null
            };

        private ChatResponseUpdate? Start(JsonElement message)
        {
            _messageId = message.TryGetProperty("id", out JsonElement id) ? id.GetString() : null;
            _modelId = message.TryGetProperty("model", out JsonElement modelElement) ? modelElement.GetString() : model;

            if (message.TryGetProperty("usage", out JsonElement usage))
            {
                _inputTokens = ReadCount(usage, "input_tokens");
                _cacheRead = ReadCount(usage, "cache_read_input_tokens");
                _cacheCreation = ReadCount(usage, "cache_creation_input_tokens");
            }

            return null;
        }

        private ChatResponseUpdate? StartBlock(JsonElement data)
        {
            JsonElement block = data.GetProperty("content_block");
            if (block.GetProperty("type").GetString() == "tool_use")
            {
                _toolCalls[data.GetProperty("index").GetInt32()] =
                    new ToolCallInProgress(block.GetProperty("id").GetString()!, block.GetProperty("name").GetString()!, new System.Text.StringBuilder());
            }

            return null;
        }

        private ChatResponseUpdate? Delta(JsonElement data)
        {
            JsonElement delta = data.GetProperty("delta");

            switch (delta.GetProperty("type").GetString())
            {
                case "text_delta":
                    return Update([new TextContent(delta.GetProperty("text").GetString())]);
                case "input_json_delta" when _toolCalls.TryGetValue(data.GetProperty("index").GetInt32(), out ToolCallInProgress? call):
                    call.Input.Append(delta.GetProperty("partial_json").GetString());
                    return null;
                default:
                    return null;
            }
        }

        private ChatResponseUpdate? StopBlock(JsonElement data)
        {
            int index = data.GetProperty("index").GetInt32();
            if (!_toolCalls.Remove(index, out ToolCallInProgress? call))
            {
                return null;
            }

            string input = call.Input.Length == 0 ? "{}" : call.Input.ToString();
            using var arguments = JsonDocument.Parse(input);

            return Update([new FunctionCallContent(
                call.Id,
                call.Name,
                arguments.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.Clone()))]);
        }

        private ChatResponseUpdate End(JsonElement data)
        {
            string? stopReason = data.GetProperty("delta").TryGetProperty("stop_reason", out JsonElement stop) ? stop.GetString() : null;
            long output = data.TryGetProperty("usage", out JsonElement usage) ? ReadCount(usage, "output_tokens") : 0;

            ChatResponseUpdate update = Update([new UsageContent(Usage(_inputTokens, output, _cacheRead, _cacheCreation))]);
            update.FinishReason = ToFinishReason(stopReason);

            return update;
        }

        private ChatResponseUpdate Update(IList<AIContent> contents) =>
            new(ChatRole.Assistant, contents)
            {
                MessageId = _messageId,
                ResponseId = _messageId,
                ModelId = _modelId
            };

        private sealed record ToolCallInProgress(string Id, string Name, System.Text.StringBuilder Input);
    }
}
