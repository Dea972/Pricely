using System.Globalization;
using System.Text.Json;
using Pricely.Web.Api;
using Pricely.Web.Assistant;
using Pricely.Web.Components.Shared;

namespace Pricely.Web.Components.Assistant;

/// <summary>
/// One line for each action of a batch (ADR-028): what it will do, read from its arguments, then what it did, read from
/// its result. Display only: the API decides and checks everything.
/// </summary>
public static class ActionSummaries
{
    /// <summary>What the action will do, in a few words.</summary>
    public static string Describe(ProposedAction action)
    {
        JsonElement arguments = action.Arguments;

        return action.ToolName switch
        {
            ToolCatalog.FindOrAddProduct => Join(
                Text(arguments, "name"),
                Text(arguments, "brand"),
                Text(arguments, "gtin") is string gtin ? "GTIN " + gtin : null,
                Path(arguments, "categoryPath")),
            ToolCatalog.FindOrAddMerchant => Join(Text(arguments, "name"), Host(arguments, "website")),
            ToolCatalog.AddWebOffer => Join(
                Price(arguments),
                Host(arguments, "pageAddress"),
                Text(arguments, "productGtin") is string productGtin ? "GTIN " + productGtin : Join(Text(arguments, "productBrand"), Text(arguments, "productReference"))),
            _ => ToolCatalog.EffectOf(action.ToolName) ?? action.ToolName
        };
    }

    /// <summary>What the executed action did, or null for another tool (its card says it).</summary>
    public static string? Outcome(ProposedAction action)
    {
        if (action.Result is not JsonElement result)
        {
            return null;
        }

        return action.ToolName switch
        {
            ToolCatalog.FindOrAddProduct when ToolCatalog.Read<FoundProductDto>(result) is { } product => ProductOutcome(product),
            ToolCatalog.FindOrAddMerchant when ToolCatalog.Read<FoundMerchantDto>(result) is { } merchant =>
                MerchantOutcome(merchant),
            ToolCatalog.AddWebOffer when ToolCatalog.Read<AddedWebOfferDto>(result) is { } offer => OfferOutcome(offer),
            _ => null
        };
    }

    private static string MerchantOutcome(FoundMerchantDto merchant)
    {
        if (merchant.IsNew)
        {
            return "Enseigne ajoutée (page de recherche à régler)";
        }

        return merchant.IsActive ? "Enseigne déjà suivie" : "Enseigne connue, désactivée";
    }

    private static string ProductOutcome(FoundProductDto product)
    {
        if (!product.IsNew)
        {
            return "Déjà au catalogue";
        }

        return product.CreatedCategories.Count > 0
            ? $"Ajouté au catalogue (typologie créée : {string.Join(" › ", product.CreatedCategories)})"
            : "Ajouté au catalogue";
    }

    private static string OfferOutcome(AddedWebOfferDto offer)
    {
        string outcome = offer.Outcome switch
        {
            "NewOfferConfirmed" => "Offre confirmée (rapprochement automatique)",
            "NewOfferProposed" => "Offre à valider",
            "ExistingOfferObserved" => "Nouveau relevé sur une offre existante",
            _ => "Offre enregistrée"
        };

        return Join(
            $"{outcome} · {Format.Price(offer.Price, offer.Currency)}",
            offer.IsFlaggedAsAnomaly ? "prix suspect" : null,
            offer.AddedToProfile ? "enseigne ajoutée au profil de la typologie" : null);
    }

    private static string? Text(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()
                : null;

    private static string? Host(JsonElement arguments, string name) =>
        Text(arguments, name) is string address && Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) ? uri.Host : null;

    private static string? Path(JsonElement arguments, string name) =>
        arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(name, out JsonElement value)
            && value.ValueKind == JsonValueKind.Array
                ? string.Join(" › ", value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()))
                : null;

    private static string? Price(JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty("price", out JsonElement price))
        {
            return null;
        }

        decimal? amount = price.ValueKind switch
        {
            JsonValueKind.Number when price.TryGetDecimal(out decimal number) => number,
            JsonValueKind.String when decimal.TryParse(price.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) => parsed,
            _ => null
        };

        return amount is decimal value ? Format.Price(value, Text(arguments, "currency") ?? "EUR") : null;
    }

    private static string Join(params string?[] parts) =>
        string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
