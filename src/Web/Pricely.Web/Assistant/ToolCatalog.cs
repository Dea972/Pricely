using System.Text.Json;
using Pricely.Web.Api;

namespace Pricely.Web.Assistant;

/// <summary>
/// The tools of the agents as the front shows them: the component of each read tool result, the title and effect of each
/// write tool. Unknown tools still appear (their name), without a typed component.
/// </summary>
public static class ToolCatalog
{
    public const string GetCategoryTree = "get_category_tree";
    public const string SearchProducts = "search_products";
    public const string GetProduct = "get_product";
    public const string ListMerchants = "list_merchants";
    public const string ComparePrices = "compare_prices";
    public const string GetPriceHistory = "get_price_history";
    public const string ListPriceAnomalies = "list_price_anomalies";
    public const string GetCollectionPlan = "get_collection_plan";
    public const string ListOffersPendingReview = "list_offers_pending_review";
    public const string CollectProductOffers = "collect_product_offers";

    public static string TitleOf(string toolName) =>
        toolName switch
        {
            CollectProductOffers => "Collecter les offres du produit",
            _ => toolName
        };

    /// <summary>What a write tool changes, said before the user decides.</summary>
    public static string? EffectOf(string toolName) =>
        toolName switch
        {
            CollectProductOffers => "Nouvelles offres à valider, nouveaux relevés de prix",
            _ => null
        };

    /// <summary>The <c>productId</c> argument of a call, if any.</summary>
    public static Guid? ProductIdOf(JsonElement? arguments) =>
        arguments is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty("productId", out JsonElement productId)
            && productId.ValueKind == JsonValueKind.String
            && productId.TryGetGuid(out Guid id)
                ? id
                : null;

    /// <summary>The result as the response type of the tool, or null when it does not have that shape.</summary>
    public static T? Read<T>(JsonElement result)
        where T : class
    {
        try
        {
            return result.Deserialize<T>(PricelyApiClient.JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
