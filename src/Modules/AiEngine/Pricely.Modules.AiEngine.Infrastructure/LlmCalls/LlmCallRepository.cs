using Pricely.Modules.AiEngine.Domain.LlmCalls;
using Pricely.Modules.AiEngine.Infrastructure.Database;

namespace Pricely.Modules.AiEngine.Infrastructure.LlmCalls;

internal sealed class LlmCallRepository(AiEngineDbContext context) : ILlmCallRepository
{
    public void Insert(LlmCall call) => context.LlmCalls.Add(call);
}
