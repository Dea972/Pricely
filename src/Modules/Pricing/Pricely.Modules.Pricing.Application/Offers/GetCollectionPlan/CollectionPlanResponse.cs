namespace Pricely.Modules.Pricing.Application.Offers.GetCollectionPlan;

/// <param name="Instructions">Collection instructions of the typology, from general to specific.</param>
public sealed record CollectionPlanResponse(
    CollectionPlanProductResponse Product,
    IReadOnlyList<CollectionPlanMerchantResponse> Merchants,
    string Instructions);

public sealed record CollectionPlanProductResponse(
    Guid ProductId,
    string Name,
    string Brand,
    string? Gtin,
    string? ManufacturerReference);

/// <param name="Priority">1 = queried first.</param>
public sealed record CollectionPlanMerchantResponse(Guid MerchantId, string Name, Uri Website, int Priority);
