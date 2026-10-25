namespace Pricely.Modules.AiEngine.Domain.LlmCalls;

public interface ILlmCallRepository
{
    void Insert(LlmCall call);
}
