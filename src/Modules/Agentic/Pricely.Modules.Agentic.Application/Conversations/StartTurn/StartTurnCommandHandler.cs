using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Abstractions.Data;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations.StartTurn;

internal sealed class StartTurnCommandHandler(
    AgentCatalog catalog,
    IAgentConversationRepository conversations,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<StartTurnCommand, TurnStartedResponse>
{
    public async Task<Result<TurnStartedResponse>> Handle(StartTurnCommand request, CancellationToken cancellationToken)
    {
        AgentDefinition? agent = catalog.FindAgent(request.AgentName);
        if (agent is null)
        {
            return Result.Failure<TurnStartedResponse>(AgentConversationErrors.UnknownAgent(request.AgentName));
        }

        if (currentUser.UserId is not Guid userId)
        {
            return Result.Failure<TurnStartedResponse>(AgentConversationErrors.UnknownUser);
        }

        if (request.ConversationId is Guid conversationId)
        {
            AgentConversation? existing = await conversations.GetAsync(conversationId, cancellationToken);
            if (existing is null || existing.EnsureOwnedBy(userId).IsFailure)
            {
                return Result.Failure<TurnStartedResponse>(AgentConversationErrors.NotFound(conversationId));
            }

            return string.Equals(existing.AgentName, agent.Name, StringComparison.Ordinal)
                ? new TurnStartedResponse(existing.Id, existing.Title)
                : Result.Failure<TurnStartedResponse>(AgentConversationErrors.AgentMismatch(existing.Id, agent.Name));
        }

        Result<AgentConversation> started = AgentConversation.Start(agent.Name, userId, request.Message, dateTimeProvider.UtcNow);
        if (started.IsFailure)
        {
            return Result.Failure<TurnStartedResponse>(started.Error);
        }

        conversations.Insert(started.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TurnStartedResponse(started.Value.Id, started.Value.Title);
    }
}
