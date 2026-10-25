using Pricely.Common.Application.Messaging;
using Pricely.Modules.AiEngine.Domain.LlmCalls;

namespace Pricely.Modules.AiEngine.Application.LlmCalls.RecordLlmCall;

/// <summary>Adds a call reported by a provider to the journal, with its frozen cost.</summary>
public sealed record RecordLlmCallCommand(LlmCallDetails Details) : ICommand;
