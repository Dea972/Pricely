using System.Diagnostics;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;
using Microsoft.Extensions.AI;
using Pricely.Common.Domain;

namespace Pricely.Common.Application.AI;

/// <summary>
/// Base of every LLM provider. Measures the call, turns provider failures into <see cref="Result"/> failures
/// and publishes <see cref="LlmCalledIntegrationEvent"/> for usage tracking.
/// </summary>
public abstract class LlmServiceBase(IEventBus eventBus, IDateTimeProvider dateTimeProvider)
{
    /// <summary>Provider name reported in the call journal (for example <c>Anthropic</c>).</summary>
    public abstract string ProviderName { get; }

    /// <summary>Model used when the request does not name one.</summary>
    protected abstract string DefaultModel { get; }

    public async Task<Result<LlmResponse>> SendAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        DateTime calledOnUtc = dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        string model = request.Model ?? DefaultModel;

        LlmResponse response;
        string? errorMessage = null;

        try
        {
            response = await SendCoreAsync(request, model, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Provider errors and HTTP timeouts (TaskCanceledException without caller cancellation) are failures.
            response = new LlmResponse(string.Empty, LlmTokenUsage.None, model);
            errorMessage = exception.Message;
        }

        stopwatch.Stop();

        // Published outside the try: a publication failure must not turn a successful (and billed) call into a failure.
        await PublishCallAsync(request, calledOnUtc, stopwatch.ElapsedMilliseconds, response, errorMessage, cancellationToken);

        return errorMessage is null
            ? response
            : Result.Failure<LlmResponse>(LlmErrors.CallFailed(ProviderName, errorMessage));
    }

    /// <summary>
    /// Chat client of this provider for a feature (multi-turn conversation with tools, ADR-008). Every call made through it
    /// is measured and published as <see cref="LlmCalledIntegrationEvent"/>, like <see cref="SendAsync"/>.
    /// </summary>
    public IChatClient CreateChatClient(LlmChatContext context)
    {
        string model = context.Model ?? DefaultModel;

        IChatClient inner = CreateChatClientCore(model)
            ?? throw new NotSupportedException($"{ProviderName} does not provide a chat client.");

        return new MeteredChatClient(
            inner,
            model,
            dateTimeProvider,
            (calledOnUtc, responseTimeMs, response, errorMessage, cancellationToken) => PublishCallAsync(
                context.Feature,
                context.SessionId,
                context.UserId,
                calledOnUtc,
                responseTimeMs,
                response,
                errorMessage,
                cancellationToken));
    }

    /// <summary>Calls the provider. Throws on failure; <see cref="SendAsync"/> turns the exception into a result.</summary>
    protected abstract Task<LlmResponse> SendCoreAsync(LlmRequest request, string model, CancellationToken cancellationToken);

    /// <summary>Raw chat client of the provider for a model, or null when the provider has none.</summary>
    protected virtual IChatClient? CreateChatClientCore(string model) => null;

    private Task PublishCallAsync(
        LlmRequest request,
        DateTime calledOnUtc,
        long responseTimeMs,
        LlmResponse response,
        string? errorMessage,
        CancellationToken cancellationToken) =>
        PublishCallAsync(
            request.Feature,
            request.SessionId,
            request.Prompt.UserId,
            calledOnUtc,
            responseTimeMs,
            response,
            errorMessage,
            cancellationToken);

    private Task PublishCallAsync(
        AiFeature feature,
        string sessionId,
        string? userId,
        DateTime calledOnUtc,
        long responseTimeMs,
        LlmResponse response,
        string? errorMessage,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new LlmCalledIntegrationEvent(Guid.CreateVersion7(), calledOnUtc)
            {
                SessionId = sessionId,
                FeatureCode = feature.Code,
                SourceModule = feature.SourceModule,
                UserId = userId,
                ModelProvider = ProviderName,
                ModelName = response.ModelName,
                InputTokens = response.TokenUsage.InputTokens,
                InputCachedTokens = response.TokenUsage.InputCachedTokens,
                InputCacheCreationTokens = response.TokenUsage.InputCacheCreationTokens,
                OutputTokens = response.TokenUsage.OutputTokens,
                OutputReasoningTokens = response.TokenUsage.OutputReasoningTokens,
                ResponseTimeMs = responseTimeMs,
                IsSuccess = errorMessage is null,
                ErrorMessage = errorMessage
            },
            cancellationToken);
}
