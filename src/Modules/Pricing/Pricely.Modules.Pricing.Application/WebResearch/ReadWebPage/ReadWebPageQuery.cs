using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.WebResearch.ReadWebPage;

/// <summary>
/// Reads a public page of the web (a search result) without keeping it: its text and the products it declares. A price
/// read this way is not verified; only a collection (a snapshot of a merchant's page) records one.
/// </summary>
public sealed record ReadWebPageQuery(Uri Address) : IQuery<WebPageResponse>
{
    public const int AddressMaxLength = 2048;
}
