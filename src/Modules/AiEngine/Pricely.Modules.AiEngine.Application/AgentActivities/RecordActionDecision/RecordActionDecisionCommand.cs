using Pricely.Common.Application.Messaging;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Application.AgentActivities.RecordActionDecision;

/// <summary>Adds a person's decision on an action proposed by an agent to the journal.</summary>
public sealed record RecordActionDecisionCommand(
    AgentActivityDetails Details,
    Guid ActionId,
    AgentActionDecision Decision,
    DateTime ProposedOnUtc) : ICommand;
