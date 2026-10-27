using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.FindOrRegisterMerchant;

/// <summary>
/// The merchant followed on this site if there is one, else a new merchant for the site (its home page) under this name
/// (ADR-028). Identified by its site, so that it can follow or precede other actions of the same batch.
/// </summary>
/// <param name="Website">Any address of the merchant's site (a product page found on the web will do).</param>
public sealed record FindOrRegisterMerchantCommand(string Name, Uri Website) : ICommand<FoundMerchantResponse>;

/// <param name="IsNew">Whether the merchant was registered by this command (false: it was already followed).</param>
public sealed record FoundMerchantResponse(Guid MerchantId, string Name, Uri Website, bool IsActive, bool IsNew);
