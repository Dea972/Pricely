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
    private bool _proposing;
    private IReadOnlyList<ModelPriceProposalDto>? _proposals;
    private ModelForm _newModel = new();

    private const string ToCheck = "ToCheck";

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

    private async Task ProposePricesAsync()
    {
        if (_saving)
        {
            return;
        }

        _saving = true;
        _proposing = true;
        Problem = null;

        try
        {
            _proposals = await CallAsync(() => Api.ProposeModelPricesAsync(CancellationToken.None));
        }
        finally
        {
            _saving = false;
            _proposing = false;
        }
    }

    private Task ApplyAsync(ModelPriceProposalDto proposal) => ApplyManyAsync([proposal]);

    private Task ApplyAllAsync() => ApplyManyAsync([.. _proposals!.Where(p => p.IsChanged && p.Proposed is not null)]);

    /// <summary>From today (UTC), with the source of the proposal; the proposals are then looked up again.</summary>
    private async Task ApplyManyAsync(IReadOnlyList<ModelPriceProposalDto> proposals)
    {
        if (_saving)
        {
            return;
        }

        _saving = true;
        Problem = null;
        var today = DateOnly.FromDateTime(Clock.GetUtcNow().UtcDateTime);

        try
        {
            bool applied = await CallAsync(async () =>
            {
                foreach (ModelPriceProposalDto proposal in proposals)
                {
                    PriceDto price = proposal.Proposed!;
                    await Api.SetModelPriceAsync(
                        proposal.ModelId,
                        new ModelPriceRequest(today, price.InputPerMillion, price.CachedInputPerMillion, price.CacheWritePerMillion, price.OutputPerMillion, proposal.Source),
                        CancellationToken.None);
                }

                return proposals;
            }) is not null;

            if (applied)
            {
                await LoadCatalogAsync();
                _proposals = [.. _proposals!.Select(p => proposals.Contains(p) ? p with { IsChanged = false, Current = p.Proposed } : p)];
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task RegisterModelAsync()
    {
        if (_saving || string.IsNullOrWhiteSpace(_newModel.ProviderModelId))
        {
            return;
        }

        _saving = true;
        Problem = null;

        try
        {
            string id = _newModel.ProviderModelId.Trim();
            string label = string.IsNullOrWhiteSpace(_newModel.DisplayName) ? id : _newModel.DisplayName.Trim();
            if (await CallAsync(async () => { await Api.RegisterModelAsync(_newModel.Provider, id, label, CancellationToken.None); return id; }) is not null)
            {
                _newModel = new ModelForm();
                await LoadCatalogAsync();
            }
        }
        finally
        {
            _saving = false;
        }
    }

    private static string PriceText(PriceDto? price) =>
        price is null
            ? "—"
            : $"entrée {Format.Usd(price.InputPerMillion)} · sortie {Format.Usd(price.OutputPerMillion)} · cache {Format.Usd(price.CachedInputPerMillion)} / {Format.Usd(price.CacheWritePerMillion)}";

    private static string SourceLabel(ModelPriceProposalDto proposal) =>
        proposal.Source switch
        {
            "LiteLlm" => "LiteLLM",
            _ => "Lu par l'IA · à vérifier"
        };

    private static string NoteLabel(string note) =>
        note switch
        {
            "NotFound" => "Ni LiteLLM ni la page de tarifs ne donnent ce modèle : saisis son tarif à la main.",
            "PriceListUnavailable" => "La liste de LiteLLM n'a pas pu être lue.",
            "CachePricesMissing" => "Pas de tarif de cache publié : le tarif d'entrée est proposé.",
            _ => note
        };

    private sealed class ModelForm
    {
        public string Provider { get; set; } = "Anthropic";

        public string ProviderModelId { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
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
