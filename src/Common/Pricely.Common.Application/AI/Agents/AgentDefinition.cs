namespace Pricely.Common.Application.AI.Agents;

/// <summary>
/// An agent, declared by the application that uses the Agentic module (like an <see cref="AiFeature"/>): its name
/// (route segment, kebab-case), the feature it calls the LLM for, its instructions (system prompt) and the names of the
/// tools it may call. Registered with <see cref="AgentServiceCollectionExtensions.AddAgent"/>.
/// </summary>
public sealed record AgentDefinition(string Name, AiFeature Feature, string Instructions, IReadOnlyCollection<string> ToolNames);
