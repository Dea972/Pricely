using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;

namespace Pricely.Common.Infrastructure.Ai.Anthropic;

/// <summary>
/// Claude through the Anthropic Messages API (<c>POST /v1/messages</c>).
/// </summary>
internal sealed class AnthropicLlmService(
    HttpClient httpClient,
    IOptions<AnthropicOptions> options,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider)
    : LlmServiceBase(eventBus, dateTimeProvider)
{
    private const string JsonResponseReminder =
        "\n\nAnswer ONLY with valid JSON, without markdown and without any text around it.";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public override string ProviderName => "Anthropic";

    protected override string DefaultModel => options.Value.DefaultModel;

    protected override IChatClient CreateChatClientCore(string model) => new AnthropicChatClient(httpClient, options, model);

    protected override async Task<LlmResponse> SendCoreAsync(
        LlmRequest request,
        string model,
        CancellationToken cancellationToken)
    {
        string message = request.Prompt.RequireJsonResponse
            ? request.Prompt.Message + JsonResponseReminder
            : request.Prompt.Message;

        MessagesRequest body = new(
            model,
            options.Value.MaxTokens,
            request.Prompt.SystemPrompt,
            [new MessageParameter("user", message)]);

        using HttpResponseMessage httpResponse = await httpClient.PostAsJsonAsync(
            new Uri("v1/messages", UriKind.Relative),
            body,
            SerializerOptions,
            cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            string error = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"Anthropic answered {(int)httpResponse.StatusCode}: {error}",
                inner: null,
                statusCode: httpResponse.StatusCode);
        }

        MessagesResponse response = await httpResponse.Content.ReadFromJsonAsync<MessagesResponse>(SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from Anthropic.");

        string content = string.Concat((response.Content ?? []).Where(block => block.Text is not null).Select(block => block.Text));

        return new LlmResponse(
            content,
            new LlmTokenUsage(
                response.Usage?.InputTokens ?? 0,
                response.Usage?.CacheReadInputTokens ?? 0,
                response.Usage?.CacheCreationInputTokens ?? 0,
                response.Usage?.OutputTokens ?? 0,
                0),
            response.Model ?? model);
    }

    private sealed record MessagesRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string? System,
        [property: JsonPropertyName("messages")] IReadOnlyList<MessageParameter> Messages);

    private sealed record MessageParameter(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record MessagesResponse(
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("content")] IReadOnlyList<ContentBlock>? Content,
        [property: JsonPropertyName("usage")] Usage? Usage);

    private sealed record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);

    private sealed record Usage(
        [property: JsonPropertyName("input_tokens")] int InputTokens,
        [property: JsonPropertyName("output_tokens")] int OutputTokens,
        [property: JsonPropertyName("cache_read_input_tokens")] int CacheReadInputTokens,
        [property: JsonPropertyName("cache_creation_input_tokens")] int CacheCreationInputTokens);
}
