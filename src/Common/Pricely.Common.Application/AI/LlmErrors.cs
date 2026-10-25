using Pricely.Common.Domain;

namespace Pricely.Common.Application.AI;

public static class LlmErrors
{
    public static Error CallFailed(string provider, string reason) =>
        Error.Problem("Llm.CallFailed", $"The call to the LLM provider '{provider}' failed: {reason}");
}
