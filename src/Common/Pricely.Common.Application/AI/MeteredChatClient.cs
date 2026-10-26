using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Pricely.Common.Application.Clock;

namespace Pricely.Common.Application.AI;

/// <summary>
/// Measures every call of a provider chat client (one per model turn, tool round-trips included) and hands it to
/// <see cref="LlmServiceBase"/> for publication, like a prompt sent through <see cref="LlmServiceBase.SendAsync"/>.
/// </summary>
internal sealed class MeteredChatClient(
    IChatClient innerClient,
    string model,
    IDateTimeProvider dateTimeProvider,
    Func<DateTime, long, LlmResponse, string?, CancellationToken, Task> publishCallAsync)
    : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        DateTime calledOnUtc = dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ChatResponse response = await base.GetResponseAsync(messages, options, cancellationToken);

            await publishCallAsync(calledOnUtc, stopwatch.ElapsedMilliseconds, ToLlmResponse(response.Usage, response.ModelId), null, cancellationToken);

            return response;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            await publishCallAsync(calledOnUtc, stopwatch.ElapsedMilliseconds, ToLlmResponse(null, null), exception.Message, cancellationToken);

            throw;
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        DateTime calledOnUtc = dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        UsageDetails? usage = null;
        string? modelId = null;

        await foreach (ChatResponseUpdate update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            modelId ??= update.ModelId;

            foreach (UsageContent content in update.Contents.OfType<UsageContent>())
            {
                usage ??= new UsageDetails();
                usage.Add(content.Details);
            }

            yield return update;
        }

        await publishCallAsync(calledOnUtc, stopwatch.ElapsedMilliseconds, ToLlmResponse(usage, modelId), null, cancellationToken);
    }

    private LlmResponse ToLlmResponse(UsageDetails? usage, string? modelId)
    {
        long cacheCreation = usage?.AdditionalCounts?.GetValueOrDefault(LlmChatUsage.CacheCreationInputTokens) ?? 0;

        return new LlmResponse(
            string.Empty,
            new LlmTokenUsage(
                (int)(usage?.InputTokenCount ?? 0),
                (int)(usage?.CachedInputTokenCount ?? 0),
                (int)cacheCreation,
                (int)(usage?.OutputTokenCount ?? 0),
                (int)(usage?.ReasoningTokenCount ?? 0)),
            modelId ?? model);
    }
}
