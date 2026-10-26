using System.Diagnostics;
using Microsoft.Extensions.AI;
using Pricely.Common.Application.AI;
using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.EventBus;

namespace Pricely.Modules.Agentic.Application.Tools;

/// <summary>
/// Publishes <see cref="AiToolInvokedIntegrationEvent"/> for every tool run for an agent (read tools during a turn,
/// write tools after approval), for the AiEngine journal.
/// </summary>
internal sealed class ToolInvocationJournal(IEventBus eventBus, IDateTimeProvider dateTimeProvider)
{
    public async Task<object?> InvokeAsync(
        AgentTool tool,
        AIFunctionArguments arguments,
        ToolCallContext context,
        CancellationToken cancellationToken)
    {
        DateTime invokedOnUtc = dateTimeProvider.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            object? result = await tool.Function.InvokeAsync(arguments, cancellationToken);
            await PublishAsync(tool, context, invokedOnUtc, stopwatch.ElapsedMilliseconds, null, cancellationToken);

            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await PublishAsync(tool, context, invokedOnUtc, stopwatch.ElapsedMilliseconds, exception.Message, CancellationToken.None);
            throw;
        }
    }

    /// <summary>The tool as given to the model during a turn: same declaration, journaled invocation.</summary>
    public AIFunction Wrap(AgentTool tool, ToolCallContext context) => new JournaledFunction(tool, context, this);

    private Task PublishAsync(
        AgentTool tool,
        ToolCallContext context,
        DateTime invokedOnUtc,
        long durationMs,
        string? errorMessage,
        CancellationToken cancellationToken) =>
        eventBus.PublishAsync(
            new AiToolInvokedIntegrationEvent(Guid.CreateVersion7(), invokedOnUtc)
            {
                SessionId = context.SessionId,
                FeatureCode = context.Feature.Code,
                SourceModule = context.Feature.SourceModule,
                AgentName = context.AgentName,
                ToolName = tool.Name,
                IsReadOnly = tool.IsReadOnly,
                WasApproved = context.WasApproved,
                UserId = context.UserId,
                DurationMs = durationMs,
                IsSuccess = errorMessage is null,
                ErrorMessage = errorMessage
            },
            cancellationToken);

    private sealed class JournaledFunction(AgentTool tool, ToolCallContext context, ToolInvocationJournal journal)
        : DelegatingAIFunction(tool.Function)
    {
        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) =>
            await journal.InvokeAsync(tool, arguments, context, cancellationToken);
    }
}

/// <summary>Who a tool runs for, for the journal.</summary>
internal sealed record ToolCallContext(AiFeature Feature, string AgentName, string SessionId, string? UserId, bool WasApproved);
