using Pricely.Common.Domain;

namespace Pricely.Modules.Agentic.Domain.Conversations;

/// <summary>
/// A conversation between a user and an agent. Only its owner can read it or continue it.
/// </summary>
public sealed class AgentConversation : Entity
{
    public const int AgentNameMaxLength = 50;

    public const int TitleMaxLength = 120;

    public const int MessageMaxLength = 8000;

    private readonly List<AgentMessage> _messages = [];

    private AgentConversation()
    {
    }

    public Guid Id { get; private set; }

    public string AgentName { get; private set; } = string.Empty;

    /// <summary>User of the application; plain identifier, no foreign key across schemas.</summary>
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTime StartedOnUtc { get; private set; }

    public DateTime LastActivityOnUtc { get; private set; }

    public IReadOnlyList<AgentMessage> Messages => _messages;

    public static Result<AgentConversation> Start(string agentName, Guid userId, string firstMessage, DateTime utcNow)
    {
        Result validation = ValidateUserMessage(firstMessage);
        if (validation.IsFailure)
        {
            return Result.Failure<AgentConversation>(validation.Error);
        }

        string title = firstMessage.Trim();

        return new AgentConversation
        {
            Id = Guid.CreateVersion7(),
            AgentName = agentName,
            UserId = userId,
            Title = title.Length <= TitleMaxLength ? title : string.Concat(title.AsSpan(0, TitleMaxLength - 1), "…"),
            StartedOnUtc = utcNow,
            LastActivityOnUtc = utcNow
        };
    }

    public static Result ValidateUserMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Result.Failure(AgentConversationErrors.EmptyMessage);
        }

        return message.Length > MessageMaxLength ? Result.Failure(AgentConversationErrors.MessageTooLong) : Result.Success();
    }

    public Result EnsureOwnedBy(Guid userId) =>
        UserId == userId ? Result.Success() : Result.Failure(AgentConversationErrors.NotFound(Id));

    public void Append(AgentMessageRole role, string content, DateTime utcNow)
    {
        _messages.Add(AgentMessage.Create(Id, _messages.Count + 1, role, content, utcNow));
        LastActivityOnUtc = utcNow;
    }
}
