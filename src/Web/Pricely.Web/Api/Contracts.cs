using System.Text.Json;

namespace Pricely.Web.Api;

// The front's own view of the API responses (ADR-015): read over HTTP, never shared with the modules. A field the
// API adds is ignored until the front needs it.

public sealed record ProductDto(Guid Id, string Name, string Brand, string? Gtin, string? ManufacturerReference, Guid ProductCategoryId);

public sealed record ProductCategoryDto(Guid Id, string Name, Guid? ParentId, int Depth);

public sealed record MerchantDto(Guid Id, string Name, string Website, bool IsActive);

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
