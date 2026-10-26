using Microsoft.Extensions.AI;
using Pricely.Common.Domain;

namespace Pricely.Common.Application.AI;

/// <summary>
/// The LLM of a feature, as resolved by <see cref="ILlmGateway"/>: provider service, target model and feature metadata.
/// </summary>
public sealed class LlmClient(LlmServiceBase service, AiFeature feature, LlmTarget target)
{
    public AiFeature Feature { get; } = feature;

    public LlmTarget Target { get; } = target;

    public Task<Result<LlmResponse>> SendAsync(LlmPrompt prompt, CancellationToken cancellationToken = default) =>
        service.SendAsync(
            new LlmRequest
            {
                Feature = Feature,
                Prompt = prompt,
                Model = Target.Model,
                SessionId = prompt.SessionId ?? Guid.CreateVersion7().ToString()
            },
            cancellationToken);

    /// <summary>
    /// Chat client of the target for a multi-turn conversation with tools (ADR-008). Each call is journaled under
    /// <paramref name="sessionId"/>.
    /// </summary>
    public IChatClient AsChatClient(string sessionId, string? userId = null, string? agentName = null) =>
        service.CreateChatClient(new LlmChatContext(Feature, Target.Model, sessionId, userId, agentName));
}
