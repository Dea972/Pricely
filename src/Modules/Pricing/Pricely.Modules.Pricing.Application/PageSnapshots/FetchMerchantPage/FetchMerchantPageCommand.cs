using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.PageSnapshots.FetchMerchantPage;

/// <summary>Reads a page of a merchant's site, or its search page for a text, and keeps the snapshot.</summary>
public sealed record FetchMerchantPageCommand(Guid MerchantId, Uri? Address, string? SearchText) : ICommand<FetchedPageResponse>;
