using Pricely.Common.Application.Messaging;
using Pricely.Modules.AiEngine.Domain.WebSearches;

namespace Pricely.Modules.AiEngine.Application.WebSearches.RecordWebSearch;

/// <summary>Adds a search reported by a searcher to the journal, with its frozen cost.</summary>
public sealed record RecordWebSearchCommand(WebSearchDetails Details) : ICommand;
