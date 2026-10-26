using System.Text.Json;

namespace Pricely.Web.Api;

// The front's own view of the API responses (ADR-015): read over HTTP, never shared with the modules. A field the
// API adds is ignored until the front needs it.

public sealed record ProductDto(Guid Id, string Name, string Brand, string? Gtin, string? ManufacturerReference, Guid ProductCategoryId);

public sealed record ProductCategoryDto(Guid Id, string Name, Guid? ParentId, int Depth);

public sealed record MerchantDto(Guid Id, string Name, string Website, bool IsActive, string? SearchTemplate = null);

/// <summary>A confirmed offer and its latest price (<c>compare_prices</c>, <c>pricing/products/{id}/prices</c>).</summary>
public sealed record MerchantPriceDto
{
    public Guid OfferId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Availability { get; init; } = string.Empty;

    public DateTime ObservedOnUtc { get; init; }

    public bool IsFlaggedAsAnomaly { get; init; }
}

public sealed record ProductOffersDto(Guid ProductId, decimal? ConfirmedMedianAmount, string? Currency, IReadOnlyList<ProductOfferDto> Offers);

public sealed record ProductOfferDto
{
    public Guid OfferId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? ConfirmationMethod { get; init; }

    public decimal? LatestAmount { get; init; }

    public string? LatestCurrency { get; init; }

    public DateTime? LatestObservedOnUtc { get; init; }

    public bool LatestIsFlaggedAsAnomaly { get; init; }

    /// <summary>Relative deviation from the confirmed median (0.05 = +5 %).</summary>
    public decimal? DeviationFromMedian { get; init; }
}

public sealed record PriceHistoryEntryDto
{
    public Guid OfferId { get; init; }

    public Guid MerchantId { get; init; }

    public string MerchantName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Availability { get; init; } = string.Empty;

    public DateTime ObservedOnUtc { get; init; }

    public bool IsFlaggedAsAnomaly { get; init; }
}

public sealed record PriceAnomalyDto
{
    public Guid PriceObservationId { get; init; }

    public Guid OfferId { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public string OfferStatus { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public DateTime ObservedOnUtc { get; init; }
}

public sealed record CollectionPlanDto(CollectionPlanProductDto Product, IReadOnlyList<CollectionPlanMerchantDto> Merchants, string Instructions);

public sealed record CollectionPlanProductDto(Guid ProductId, string Name, string Brand, string? Gtin, string? ManufacturerReference);

public sealed record CollectionPlanMerchantDto(Guid MerchantId, string Name, Uri Website, int Priority);

public sealed record PendingOfferDto
{
    public Guid OfferId { get; init; }

    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string MerchantName { get; init; } = string.Empty;

    public string ProductPage { get; init; } = string.Empty;

    public string? MerchantProductReference { get; init; }

    public DateTime ProposedOnUtc { get; init; }

    public decimal? LatestAmount { get; init; }

    public string? LatestCurrency { get; init; }

    public string? ProductBrand { get; init; }

    public string? ProductGtin { get; init; }

    public string? ProductManufacturerReference { get; init; }

    public string? ObservedTitle { get; init; }

    public string? ObservedGtin { get; init; }

    public string? ObservedBrand { get; init; }

    public string? ObservedManufacturerReference { get; init; }

    /// <summary>Similarity given by the collector (0 to 1), if any.</summary>
    public decimal? AiSimilarity { get; init; }

    public string? AiRationale { get; init; }

    /// <summary>Comparison of the matching policy: null when one side is missing, else whether they are the same.</summary>
    public bool? SameGtin { get; init; }

    public bool? SameBrand { get; init; }

    public bool? SameManufacturerReference { get; init; }
}

public sealed record CollectResultDto(int CollectedCount, int ProposedCount, int AutoConfirmedCount, int ObservedCount, int IgnoredCount, int AnomalyCount);

public sealed record ConversationSummaryDto(Guid Id, string AgentName, string Title, DateTime StartedOnUtc, DateTime LastActivityOnUtc);

public sealed record ConversationDto(
    Guid Id,
    string AgentName,
    string Title,
    DateTime StartedOnUtc,
    IReadOnlyList<ConversationMessageDto> Messages,
    IReadOnlyList<PendingActionDto> Actions);

public sealed record ConversationMessageDto(
    int Sequence,
    string Role,
    string Text,
    IReadOnlyList<ToolCallDto> ToolCalls,
    IReadOnlyList<ToolResultDto> ToolResults,
    DateTime CreatedOnUtc);

public sealed record ToolCallDto(string CallId, string Name, bool IsReadOnly, JsonElement? Arguments);

public sealed record ToolResultDto(string CallId, string Name, JsonElement? Result, string? Error);

public sealed record PendingActionDto
{
    public Guid Id { get; init; }

    public Guid ConversationId { get; init; }

    public string ToolName { get; init; } = string.Empty;

    public JsonElement Arguments { get; init; }

    public string RequiredScope { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime ProposedOnUtc { get; init; }

    public DateTime? DecidedOnUtc { get; init; }

    public JsonElement? Result { get; init; }

    public string? Error { get; init; }
}

public sealed record AgentUsageDto(string AgentName, int Calls, int FailedCalls, long TotalTokens, decimal Cost);

/// <summary>Usage totals of AiEngine (summary, a session, a day, a feature); costs in USD.</summary>
public record UsageDto
{
    public int Calls { get; init; }

    public int FailedCalls { get; init; }

    public int UnpricedCalls { get; init; }

    public long InputTokens { get; init; }

    public long OutputTokens { get; init; }

    public long TotalTokens { get; init; }

    public decimal Cost { get; init; }
}

public sealed record DailyUsageDto : UsageDto
{
    public DateTime Day { get; init; }
}

public sealed record FeatureUsageDto : UsageDto
{
    public string FeatureCode { get; init; } = string.Empty;
}

/// <param name="Source">0: its own target, 1: the default target, 2: no target (the provider's default model).</param>
public sealed record AiFeatureDto(string Code, string Label, string? SourceModule, string? DefaultProvider, Guid? OwnModelId, Guid? EffectiveModelId, int Source)
{
    public const string DefaultCode = "default";
}

public sealed record AiModelDto
{
    public Guid Id { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string ProviderModelId { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public DateTime? PriceValidFrom { get; init; }

    public decimal? InputPerMillion { get; init; }

    public decimal? CachedInputPerMillion { get; init; }

    public decimal? CacheWritePerMillion { get; init; }

    public decimal? OutputPerMillion { get; init; }
}

/// <param name="Source"><c>Manual</c>, or the proposal applied: <c>LiteLlm</c>, <c>AiReading</c>.</param>
public sealed record ModelPriceRequest(DateOnly ValidFrom, decimal InputPerMillion, decimal CachedInputPerMillion, decimal CacheWritePerMillion, decimal OutputPerMillion, string? Source = null);

/// <summary>A price found for a model of the catalogue (<c>aiengine/models/price-proposals</c>), nothing saved yet.</summary>
public sealed record ModelPriceProposalDto(
    Guid ModelId,
    string Provider,
    string ProviderModelId,
    PriceDto? Current,
    PriceDto? Proposed,
    string? Source,
    bool IsChanged,
    IReadOnlyList<string> Notes);

public sealed record PriceDto(decimal InputPerMillion, decimal CachedInputPerMillion, decimal CacheWritePerMillion, decimal OutputPerMillion, DateOnly? ValidFrom, string? Source);

public sealed record CollectionProfileSummaryDto(Guid ProductCategoryId, string? CategoryName, int PreferredMerchantCount, bool HasInstructions);

public sealed record EffectiveCollectionProfileDto(
    IReadOnlyList<Guid> PreferredMerchantIds,
    Guid? PreferredMerchantsSourceCategoryId,
    string Instructions,
    IReadOnlyList<InstructionBlockDto> InstructionBlocks,
    bool HasOwnProfile,
    IReadOnlyList<Guid> OwnPreferredMerchantIds,
    string? OwnInstructions);

public sealed record InstructionBlockDto(Guid ProductCategoryId, string Instructions);

public sealed record ProductRequest(string Name, string Brand, string? Gtin, string? ManufacturerReference, Guid ProductCategoryId);

/// <summary>A reference product proposed from a product page (<c>catalog/products/suggestions</c>), nothing saved yet.</summary>
public sealed record ProductSuggestionDto(
    string ProductPage,
    string? Name,
    string? Brand,
    string? Gtin,
    string? ManufacturerReference,
    Guid? ProductCategoryId,
    string? Packaging,
    string? Rationale,
    Guid? ExistingProductId,
    string? ExistingProductName,
    bool IsAiAnswer);

public sealed record MerchantRequest(string Name, string Website, string? SearchTemplate);

/// <summary>The setup of a merchant proposed from its site (<c>pricing/merchants/suggestions</c>), nothing saved yet.</summary>
public sealed record MerchantSuggestionDto(
    string Name,
    string Website,
    string? SearchTemplate,
    IReadOnlyList<string> SearchCandidates,
    SearchCheckDto? SearchCheck,
    IReadOnlyList<SuggestedCategoryDto> Categories,
    string? Rationale,
    bool IsAiAnswer);

public sealed record SearchCheckDto(string Query, string? TestedAddress, bool IsReachable, bool ShowsQuery, int TextLength);

public sealed record SuggestedCategoryDto(Guid ProductCategoryId, string Name, string? Reason);

public sealed record CollectionProfileRequest(IReadOnlyList<Guid> PreferredMerchantIds, string? Instructions);

/// <summary>Values of <c>Status</c> and <c>ConfirmationMethod</c> as the API writes them.</summary>
public static class ApiValues
{
    public const string Proposed = "Proposed";
    public const string Confirmed = "Confirmed";

    public const string Gtin = "Gtin";
    public const string ManufacturerReference = "ManufacturerReference";
    public const string Manual = "Manual";

    public const string InStock = "InStock";
    public const string OutOfStock = "OutOfStock";

    public const string Pending = "Pending";
    public const string Executed = "Executed";
    public const string Failed = "Failed";
    public const string Rejected = "Rejected";
}
