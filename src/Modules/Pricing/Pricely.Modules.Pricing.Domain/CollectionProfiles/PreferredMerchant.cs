namespace Pricely.Modules.Pricing.Domain.CollectionProfiles;

/// <summary>
/// A merchant to query first for a product typology. Priority 1 is queried first.
/// </summary>
public sealed class PreferredMerchant
{
    private PreferredMerchant()
    {
    }

    internal PreferredMerchant(Guid merchantId, int priority)
    {
        MerchantId = merchantId;
        Priority = priority;
    }

    public Guid MerchantId { get; private set; }

    public int Priority { get; private set; }

    internal void ChangePriority(int priority) => Priority = priority;
}
