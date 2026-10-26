using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Pages;

public enum HistoryPeriod
{
    OneMonth,
    ThreeMonths,
    OneYear
}

/// <summary>
/// A product: its offers by merchant, the price history and the flagged prices, everything as the API computes it
/// (median, deviation, price guard). « Collecter les offres » sends the command directly.
/// </summary>
public partial class ProductPage
{
    private ProductDto? _product;
    private string? _categoryPath;
    private ProductOffersDto? _offers;
    private IReadOnlyList<MerchantPriceDto>? _prices;
    private IReadOnlyList<PriceHistoryEntryDto>? _history;
    private int? _activeMerchants;
    private HistoryPeriod _period = HistoryPeriod.ThreeMonths;
    private CollectResultDto? _collected;
    private bool _collecting;

    [Parameter]
    public Guid ProductId { get; set; }

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    [Inject]
    private ProductLookup Products { get; set; } = default!;

    [Inject]
    private TimeProvider Clock { get; set; } = default!;

    /// <summary>The first offer of the API (cheapest first), unless its price is suspect.</summary>
    private Guid? BestOfferId => _prices is [MerchantPriceDto first, ..] && !first.IsFlaggedAsAnomaly ? first.OfferId : null;

    private string AskAssistantAddress =>
        "/?prompt=" + Uri.EscapeDataString($"Compare les prix de « {_product?.Name} » et dis-moi s'il y a des prix suspects.");

    protected override async Task OnParametersSetAsync()
    {
        Guid productId = ProductId;
        if (_product?.Id == productId)
        {
            return;
        }

        _product = null;
        _offers = null;
        _prices = null;
        _history = null;
        _collected = null;
        Problem = null;

        // After each call: the user may have opened another product in the meantime (its own load runs).
        ProductDto? product = await CallAsync(() => Api.GetProductAsync(productId, CancellationToken.None));
        string? categoryPath = product is null ? null : await Products.CategoryPathAsync(product.ProductCategoryId);
        if (product is null || productId != ProductId)
        {
            return;
        }

        (_product, _categoryPath) = (product, categoryPath);

        if (await LoadOffersAsync(productId) && await LoadHistoryAsync(productId))
        {
            IReadOnlyList<MerchantDto>? merchants = await CallAsync(() => Api.GetMerchantsAsync(CancellationToken.None));
            _activeMerchants = productId == ProductId ? merchants?.Count(m => m.IsActive) : _activeMerchants;
        }
    }

    /// <summary>False when the page shows another product by now.</summary>
    private async Task<bool> LoadOffersAsync(Guid productId)
    {
        ProductOffersDto? offers = await CallAsync(() => Api.GetProductOffersAsync(productId, CancellationToken.None));
        IReadOnlyList<MerchantPriceDto>? prices = await CallAsync(() => Api.GetPriceComparisonAsync(productId, CancellationToken.None));
        if (productId != ProductId)
        {
            return false;
        }

        (_offers, _prices) = (offers, prices);

        return true;
    }

    private async Task<bool> LoadHistoryAsync(Guid productId)
    {
        DateTime now = Clock.GetUtcNow().UtcDateTime;
        DateTime from = _period switch
        {
            HistoryPeriod.OneMonth => now.AddMonths(-1),
            HistoryPeriod.OneYear => now.AddYears(-1),
            _ => now.AddMonths(-3)
        };

        _history = null;
        IReadOnlyList<PriceHistoryEntryDto> history = await CallAsync(() => Api.GetPriceHistoryAsync(productId, from, CancellationToken.None)) ?? [];
        if (productId != ProductId)
        {
            return false;
        }

        _history = history;

        return true;
    }

    private async Task ChangePeriodAsync(HistoryPeriod period)
    {
        if (period == _period)
        {
            return;
        }

        _period = period;
        Problem = null;
        await LoadHistoryAsync(ProductId);
    }

    private async Task CollectAsync()
    {
        // Not idempotent and costly: a second click that arrives before the button is disabled does nothing.
        if (_collecting)
        {
            return;
        }

        Guid productId = ProductId;
        _collecting = true;
        _collected = null;
        Problem = null;

        try
        {
            _collected = await CallAsync(() => Api.CollectProductOffersAsync(productId, CancellationToken.None));
            if (_collected is not null && await LoadOffersAsync(productId))
            {
                await LoadHistoryAsync(productId);
            }
        }
        finally
        {
            _collecting = false;
        }
    }

    private static string Label(HistoryPeriod period) =>
        period switch
        {
            HistoryPeriod.OneMonth => "1 mois",
            HistoryPeriod.OneYear => "1 an",
            _ => "3 mois"
        };
}
