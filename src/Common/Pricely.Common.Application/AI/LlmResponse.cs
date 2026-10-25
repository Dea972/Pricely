namespace Pricely.Common.Application.AI;

public sealed record LlmResponse(string Content, LlmTokenUsage TokenUsage, string ModelName);
