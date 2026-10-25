using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.AiEngine.Application.Models.RegisterModel;

/// <param name="Provider">Provider name (<c>Anthropic</c>).</param>
/// <param name="ProviderModelId">Identifier of the model at the provider (<c>claude-sonnet-5-5</c>).</param>
public sealed record RegisterModelCommand(string Provider, string ProviderModelId, string Label) : ICommand<Guid>;
