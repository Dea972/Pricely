using Pricely.Common.Application.Messaging;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Application.AgentActivities.RecordToolInvocation;

/// <summary>Adds a tool call made for an agent to the journal.</summary>
public sealed record RecordToolInvocationCommand(
    AgentActivityDetails Details,
    bool IsReadOnly,
    bool WasApproved,
    long DurationMs,
    string? ErrorMessage) : ICommand;
