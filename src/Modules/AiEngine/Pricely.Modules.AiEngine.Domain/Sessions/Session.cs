using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Sessions;

/// <summary>
/// Unit of AI work that groups LLM calls (a conversation, a collection run...). Volumes are computed from its calls.
/// </summary>
public sealed class Session : Entity
{
    public const int ExternalIdMaxLength = 100;

    private Session()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Identifier given by the caller (<c>LlmPrompt.SessionId</c>), unique.</summary>
    public string ExternalId { get; private set; }

    /// <summary>Module that opened the session.</summary>
    public string SourceModule { get; private set; }

    public string? UserId { get; private set; }

    public DateTime StartedOnUtc { get; private set; }

    public static Session Start(string externalId, string sourceModule, string? userId, DateTime startedOnUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ExternalId = externalId,
            SourceModule = sourceModule,
            UserId = userId,
            StartedOnUtc = startedOnUtc
        };
}
