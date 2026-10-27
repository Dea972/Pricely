using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Agentic.Application.Actions;
using Pricely.Modules.Agentic.Application.Agents;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations.GetConversation;

internal sealed class GetConversationQueryHandler(IConversationReadStore readStore, AgentCatalog catalog)
    : IQueryHandler<GetConversationQuery, ConversationResponse>
{
    public async Task<Result<ConversationResponse>> Handle(GetConversationQuery request, CancellationToken cancellationToken)
    {
        ConversationDetails? conversation = await readStore.GetDetailsAsync(request.UserId, request.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<ConversationResponse>(AgentConversationErrors.NotFound(request.ConversationId));
        }

        return new ConversationResponse(
            conversation.Id,
            conversation.AgentName,
            conversation.Title,
            conversation.StartedOnUtc,
            ConversationProjection.ToMessages(conversation.Messages.Select(m => (m.Sequence, m.Role, m.Content, m.CreatedOnUtc)), catalog),
            conversation.Actions
                .Select(a => new PendingActionResponse(
                    a.Id,
                    conversation.Id,
                    a.ToolName,
                    PendingActionResponse.Parse(a.Arguments)!.Value,
                    a.RequiredScope,
                    a.Status,
                    a.ProposedOnUtc,
                    a.DecidedOnUtc,
                    PendingActionResponse.Parse(a.Result),
                    a.Error))
                .ToList());
    }
}
