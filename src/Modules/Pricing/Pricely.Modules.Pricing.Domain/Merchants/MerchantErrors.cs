using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Merchants;

public static class MerchantErrors
{
    public static readonly Error InvalidName = Error.Problem(
        "Merchants.InvalidName",
        $"The merchant name is required and cannot exceed {Merchant.NameMaxLength} characters");

    public static Error InvalidWebsite(string website) =>
        Error.Problem("Merchants.InvalidWebsite", $"'{website}' is not a valid absolute http(s) address");

    public static Error InvalidSearchTemplate(string template) =>
        Error.Problem(
            "Merchants.InvalidSearchTemplate",
            $"'{template}' is not a search address of the merchant's site containing {Merchant.QueryPlaceholder} (at most {Merchant.SearchTemplateMaxLength} characters)");

    public static Error InvalidProductPageTemplate(string template) =>
        Error.Problem(
            "Merchants.InvalidProductPageTemplate",
            $"'{template}' is not a product page address of the merchant's site containing {Merchant.Gtin13Placeholder} or {Merchant.Gtin14Placeholder} (at most {Merchant.ProductPageTemplateMaxLength} characters)");

    public static Error AddressOutsideMerchant(Uri address) =>
        Error.Problem("Merchants.AddressOutsideMerchant", $"The address {address} is not on the merchant's site");

    public static Error Inactive(Guid merchantId) =>
        Error.Problem("Merchants.Inactive", $"The merchant {merchantId} is not active");

    public static Error NameUsedByAnotherSite(string name, Uri website) =>
        Error.Conflict("Merchants.NameUsedByAnotherSite", $"The merchant name '{name}' is already used by {website}");

    public static Error NoMerchantForSite(Uri address) =>
        Error.NotFound("Merchants.NoMerchantForSite", $"No merchant is followed on the site of {address}: add it first");

    public static Error NotFound(Guid merchantId) =>
        Error.NotFound("Merchants.NotFound", $"The merchant with the identifier {merchantId} was not found");
}
