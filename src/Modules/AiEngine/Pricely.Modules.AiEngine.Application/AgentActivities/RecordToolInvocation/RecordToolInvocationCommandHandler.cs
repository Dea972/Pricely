using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Abstractions.Data;
using Pricely.Modules.AiEngine.Domain.AgentActivities;

namespace Pricely.Modules.AiEngine.Application.AgentActivities.RecordToolInvocation;

internal sealed class RecordToolInvocationCommandHandler(IAgentActivityRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordToolInvocationCommand>
{
    public async Task<Result> Handle(RecordToolInvocationCommand request, CancellationToken cancellationToken)
    {
        repository.Insert(AgentActivity.RecordToolInvocation(
            request.Details,
            request.IsReadOnly,
            request.WasApproved,
            request.DurationMs,
            request.ErrorMessage));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
