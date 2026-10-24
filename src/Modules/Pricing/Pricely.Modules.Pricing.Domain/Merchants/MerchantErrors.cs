using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.Merchants;

public static class MerchantErrors
{
    public static readonly Error InvalidName = Error.Problem(
        "Merchants.InvalidName",
        $"The merchant name is required and cannot exceed {Merchant.NameMaxLength} characters");

    public static Error InvalidWebsite(string website) =>
        Error.Problem("Merchants.InvalidWebsite", $"'{website}' is not a valid absolute http(s) address");

    public static Error NotFound(Guid merchantId) =>
        Error.NotFound("Merchants.NotFound", $"The merchant with the identifier {merchantId} was not found");
}
