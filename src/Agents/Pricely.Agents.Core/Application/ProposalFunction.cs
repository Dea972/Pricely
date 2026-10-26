using Microsoft.Extensions.AI;
using Pricely.Agents.Core.Domain;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// Stands for a write tool during a turn: the model sees the real tool (name, description, schema), but calling it only
/// records a <see cref="PendingAction"/> and tells the model that the user has to approve it (ADR-008).
/// </summary>
internal sealed class ProposalFunction(
    AgentTool tool,
    Func<AgentTool, AIFunctionArguments, CancellationToken, Task<PendingAction>> propose)
    : DelegatingAIFunction(tool.Function)
{
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        PendingAction action = await propose(tool, arguments, cancellationToken);

        return new
        {
            status = "pending_approval",
            actionId = action.Id,
            message = "Action proposée à l'utilisateur, qui doit la valider ou la refuser. Elle n'est pas encore exécutée : ne la relance pas."
        };
    }
}
