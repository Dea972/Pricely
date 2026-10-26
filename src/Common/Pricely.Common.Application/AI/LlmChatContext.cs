namespace Pricely.Common.Application.AI;

/// <summary>
/// What a chat client needs to journal its calls: the feature, the model of the provider (default model when null),
/// the usage session (conversation), the user on whose behalf it runs and the agent, if any.
/// </summary>
public sealed record LlmChatContext(AiFeature Feature, string? Model, string SessionId, string? UserId, string? AgentName = null);
