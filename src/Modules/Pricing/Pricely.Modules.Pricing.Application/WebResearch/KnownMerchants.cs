using Pricely.Modules.Pricing.Application.Merchants;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Domain.Merchants;

namespace Pricely.Modules.Pricing.Application.WebResearch;

/// <summary>Finds the merchant, already followed by Pricely, whose site a page of the web belongs to.</summary>
internal sealed class KnownMerchants(IReadOnlyCollection<MerchantResponse> merchants)
{
    public static async Task<KnownMerchants> LoadAsync(IMerchantReadStore merchantReadStore, CancellationToken cancellationToken) =>
        new(await merchantReadStore.GetAllAsync(cancellationToken));

    public KnownMerchant? Find(Uri address)
    {
        foreach (MerchantResponse merchant in merchants)
        {
            if (Uri.TryCreate(merchant.Website, UriKind.Absolute, out Uri? website) && Merchant.BelongsToSite(website, address))
            {
                return new KnownMerchant(merchant.Id, merchant.Name, merchant.IsActive);
            }
        }

        return null;
    }
}

/// <summary>A merchant followed by Pricely, whose site the page belongs to.</summary>
public sealed record KnownMerchant(Guid Id, string Name, bool IsActive);
