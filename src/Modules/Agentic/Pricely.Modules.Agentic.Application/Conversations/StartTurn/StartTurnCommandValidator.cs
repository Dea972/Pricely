using FluentValidation;
using Pricely.Modules.Agentic.Domain.Conversations;

namespace Pricely.Modules.Agentic.Application.Conversations.StartTurn;

internal sealed class StartTurnCommandValidator : AbstractValidator<StartTurnCommand>
{
    public StartTurnCommandValidator()
    {
        RuleFor(c => c.AgentName).NotEmpty().MaximumLength(AgentConversation.AgentNameMaxLength);
        RuleFor(c => c.Message).NotEmpty().MaximumLength(AgentConversation.MessageMaxLength);
    }
}
