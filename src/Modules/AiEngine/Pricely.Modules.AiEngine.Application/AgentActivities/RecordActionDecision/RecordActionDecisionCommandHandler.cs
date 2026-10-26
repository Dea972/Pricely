using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Application.AgentActivities.RecordActionDecision;

internal sealed class RecordActionDecisionCommandHandler(IAgentActivityRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordActionDecisionCommand>
{
    public async Task<Result> Handle(RecordActionDecisionCommand request, CancellationToken cancellationToken)
    {
        repository.Insert(AgentActivity.RecordDecision(request.Details, request.ActionId, request.Decision, request.ProposedOnUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
