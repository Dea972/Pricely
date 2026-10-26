using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;

/// <summary>
/// Adds a merchant, last by priority, to the collection profile of each typology. A typology that inherits its merchants
/// gets its own list: the inherited merchants, then this one (it keeps querying them).
/// </summary>
public sealed record AddMerchantToProfilesCommand(Guid MerchantId, IReadOnlyList<Guid> ProductCategoryIds) : ICommand;
