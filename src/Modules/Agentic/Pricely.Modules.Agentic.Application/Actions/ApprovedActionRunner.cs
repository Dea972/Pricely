using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Application.Conversations;
using Pricely.Modules.Agentic.Application.Tools;
using Pricely.Modules.Agentic.Domain.Actions;

namespace Pricely.Modules.Agentic.Application.Actions;

/// <summary>
/// Runs approved actions: the tool directly, with the stored arguments, without going back to the model; then saves the
/// outcome. Shared by the approval of one action and of a batch.
/// </summary>
internal sealed class ApprovedActionRunner(
    AgentCatalog catalog,
    ToolInvocationJournal journal,
    IUnitOfWork unitOfWork,
    IServiceProvider services,
    ILogger<ApprovedActionRunner> logger)
{
    /// <summary>The tool and the agent of the action, if the agent still declares this write tool.</summary>
    public Result<(AgentTool Tool, AgentDefinition Agent)> Resolve(PendingAction action)
    {
        AgentTool? tool = catalog.FindTool(action.ToolName);
        AgentDefinition? agent = catalog.FindAgent(action.AgentName);

        return tool is null || agent is null || tool.IsReadOnly || !catalog.IsToolOf(action.AgentName, action.ToolName)
            ? Result.Failure<(AgentTool, AgentDefinition)>(PendingActionErrors.UnknownTool(action.ToolName))
            : (tool, agent);
    }

    /// <summary>Runs an action already approved and saved as such, then saves its outcome.</summary>
    public async Task RunAsync(PendingAction action, AgentTool tool, AgentDefinition agent)
    {
        var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(action.Arguments)!
            .ToDictionary(a => a.Key, a => (object?)a.Value, StringComparer.Ordinal);
        var context = new ToolCallContext(agent.Feature, agent.Name, action.ConversationId.ToString(), action.UserId.ToString(), WasApproved: true);

        try
        {
            object? result = await journal.InvokeAsync(tool, new AIFunctionArguments(arguments) { Services = services }, context, CancellationToken.None);
            action.MarkExecuted(ChatMessageSerializer.SerializeValue(result));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The approved action {ActionId} ({ToolName}) failed", action.Id, action.ToolName);
            action.MarkFailed(exception.Message);
        }

        // The tool has run: its outcome is saved even if the caller went away. Nobody else writes an approved action.
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}
