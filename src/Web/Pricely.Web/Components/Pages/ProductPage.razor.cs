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

    private string AskAssistantAddress =>
        "/?prompt=" + Uri.EscapeDataString($"Compare les prix de « {_product?.Name} » et dis-moi s'il y a des prix suspects.");

    protected override async Task OnParametersSetAsync()
    {
        if (_product?.Id == ProductId)
        {
            return;
        }

        _product = null;
        _offers = null;
        _history = null;
        _collected = null;

        _product = await CallAsync(() => Api.GetProductAsync(ProductId, CancellationToken.None));
        if (_product is null)
        {
            return;
        }

        _categoryPath = await Products.CategoryPathAsync(_product.ProductCategoryId);
        await LoadOffersAsync();
        await LoadHistoryAsync();

        IReadOnlyList<MerchantDto>? merchants = await CallAsync(() => Api.GetMerchantsAsync(CancellationToken.None));
        _activeMerchants = merchants?.Count(m => m.IsActive);
    }

    private async Task LoadOffersAsync()
    {
        _offers = await CallAsync(() => Api.GetProductOffersAsync(ProductId, CancellationToken.None));
        _prices = await CallAsync(() => Api.GetPriceComparisonAsync(ProductId, CancellationToken.None));
    }

    private async Task LoadHistoryAsync()
    {
        DateTime now = Clock.GetUtcNow().UtcDateTime;
        DateTime from = _period switch
        {
            HistoryPeriod.OneMonth => now.AddMonths(-1),
            HistoryPeriod.OneYear => now.AddYears(-1),
            _ => now.AddMonths(-3)
        };

        _history = null;
        _history = await CallAsync(() => Api.GetPriceHistoryAsync(ProductId, from, CancellationToken.None)) ?? [];
    }

    private async Task ChangePeriodAsync(HistoryPeriod period)
    {
        if (period == _period)
        {
            return;
        }

        _period = period;
        await LoadHistoryAsync();
    }

    private async Task CollectAsync()
    {
        _collecting = true;
        _collected = null;

        try
        {
            _collected = await CallAsync(() => Api.CollectProductOffersAsync(ProductId, CancellationToken.None));
            if (_collected is not null)
            {
                await LoadOffersAsync();
                await LoadHistoryAsync();
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
