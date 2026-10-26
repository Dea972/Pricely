using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pricely.Web.Api;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Pages;

public enum UsagePeriod
{
    SevenDays,
    ThirtyDays,
    CurrentMonth
}

/// <summary>
/// The usage and the costs of the LLM calls (docs/design/mockups/ai-usage.html), the model targeted by each AI feature
/// and the price catalogue of the models. Every figure comes from AiEngine.
/// </summary>
public partial class AiUsagePage
{
    private static readonly UsagePeriod[] Periods = [UsagePeriod.SevenDays, UsagePeriod.ThirtyDays, UsagePeriod.CurrentMonth];

    private UsagePeriod _period = UsagePeriod.ThirtyDays;
    private UsageDto? _summary;
    private IReadOnlyList<DailyUsageDto>? _daily;
    private IReadOnlyList<FeatureUsageDto> _featureUsage = [];
    private IReadOnlyList<AiFeatureDto> _features = [];
    private IReadOnlyList<AiModelDto> _models = [];
    private AiModelDto? _priceModel;
    private PriceForm _price = new();
    private bool _saving;

    [Inject]
    private PricelyApiClient Api { get; set; } = default!;

    [Inject]
    private TimeProvider Clock { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadCatalogAsync();
        await LoadUsageAsync();
    }

    private static string Label(UsagePeriod period) =>
        period switch
        {
            UsagePeriod.SevenDays => "7 jours",
            UsagePeriod.ThirtyDays => "30 jours",
            _ => "Mois en cours"
        };

    private static string Percent(decimal value, decimal max) =>
        (value / max * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

    private DateTime FromUtc()
    {
        DateTime today = Clock.GetUtcNow().UtcDateTime.Date;

        return _period switch
        {
            UsagePeriod.SevenDays => today.AddDays(-6),
            UsagePeriod.ThirtyDays => today.AddDays(-29),
            _ => new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private async Task SelectAsync(UsagePeriod period)
    {
        _period = period;
        await LoadUsageAsync();
    }

    private async Task LoadUsageAsync()
    {
        DateTime fromUtc = FromUtc();
        Problem = null;
        _summary = await CallAsync(() => Api.GetUsageSummaryAsync(fromUtc, CancellationToken.None));
        _daily = await CallAsync(() => Api.GetDailyUsageAsync(fromUtc, CancellationToken.None));
        _featureUsage = await CallAsync(() => Api.GetUsageByFeatureAsync(fromUtc, CancellationToken.None)) ?? [];
    }

    private async Task LoadCatalogAsync()
    {
        _models = await CallAsync(() => Api.GetModelsAsync(CancellationToken.None)) ?? [];
        _features = await CallAsync(() => Api.GetAiFeaturesAsync(CancellationToken.None)) ?? [];
    }

    private string FeatureCost(string featureCode)
    {
        if (string.Equals(featureCode, AiFeatureDto.DefaultCode, StringComparison.Ordinal))
        {
            return "—";
        }

        decimal cost = _featureUsage.FirstOrDefault(u => string.Equals(u.FeatureCode, featureCode, StringComparison.Ordinal))?.Cost ?? 0;

        return Format.Cost(cost);
    }

    private string NoTarget(AiFeatureDto feature)
    {
        if (string.Equals(feature.Code, AiFeatureDto.DefaultCode, StringComparison.Ordinal))
        {
            return "aucune cible (modèle par défaut du fournisseur)";
        }

        return feature.EffectiveModelId is Guid id ? $"hérite de default ({ModelName(id)})" : "hérite de default";
    }

    private string ModelName(Guid modelId) => _models.FirstOrDefault(m => m.Id == modelId)?.ProviderModelId ?? "modèle inconnu";

    private async Task TargetAsync(AiFeatureDto feature, string? modelId)
    {
        _saving = true;
        Problem = null;

        try
        {
            Func<Task> change = Guid.TryParse(modelId, out Guid id)
                ? () => Api.SetFeatureTargetAsync(feature.Code, id, CancellationToken.None)
                : () => Api.ResetFeatureTargetAsync(feature.Code, CancellationToken.None);

            await CallAsync(async () => { await change(); return feature; });
            _features = await CallAsync(() => Api.GetAiFeaturesAsync(CancellationToken.None)) ?? _features;
        }
        finally
        {
            _saving = false;
        }
    }

    private void EditPrice(AiModelDto model)
    {
        _priceModel = model;
        _price = new PriceForm
        {
            ValidFrom = DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime),
            InputPerMillion = model.InputPerMillion ?? 0,
            CachedInputPerMillion = model.CachedInputPerMillion ?? 0,
            CacheWritePerMillion = model.CacheWritePerMillion ?? 0,
            OutputPerMillion = model.OutputPerMillion ?? 0
        };
    }

    private async Task SavePriceAsync()
    {
        if (_priceModel is not { } model || _saving)
        {
            return;
        }

        _saving = true;
        Problem = null;

        try
        {
            var price = new ModelPriceRequest(_price.ValidFrom, _price.InputPerMillion, _price.CachedInputPerMillion, _price.CacheWritePerMillion, _price.OutputPerMillion);
            if (await CallAsync(async () => { await Api.SetModelPriceAsync(model.Id, price, CancellationToken.None); return price; }) is not null)
            {
                _priceModel = null;
                await LoadCatalogAsync();
                await LoadUsageAsync();
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private sealed class PriceForm
    {
        public DateOnly ValidFrom { get; set; }

        public decimal InputPerMillion { get; set; }

        public decimal CachedInputPerMillion { get; set; }

        public decimal CacheWritePerMillion { get; set; }

        public decimal OutputPerMillion { get; set; }
    }
}
