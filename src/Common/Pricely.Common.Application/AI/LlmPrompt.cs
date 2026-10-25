namespace Pricely.Common.Application.AI;

/// <summary>
/// What a feature sends to its LLM.
/// </summary>
public sealed class LlmPrompt
{
    public required string Message { get; init; }

    public string? SystemPrompt { get; init; }

    /// <summary>Asks the model to answer with JSON only.</summary>
    public bool RequireJsonResponse { get; init; }

    /// <summary>Groups several calls into one usage session (conversation, batch...). A new session per call when null.</summary>
    public string? SessionId { get; init; }

    /// <summary>User on whose behalf the call is made, if any.</summary>
    public string? UserId { get; init; }
}
