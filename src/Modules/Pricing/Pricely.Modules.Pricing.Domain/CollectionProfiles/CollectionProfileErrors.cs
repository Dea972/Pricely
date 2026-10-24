using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.CollectionProfiles;

public static class CollectionProfileErrors
{
    public static readonly Error DuplicatePreferredMerchant = Error.Problem(
        "CollectionProfiles.DuplicatePreferredMerchant",
        "A merchant cannot appear twice in the preferred merchants");

    public static readonly Error InstructionsTooLong = Error.Problem(
        "CollectionProfiles.InstructionsTooLong",
        $"The collection instructions cannot exceed {CollectionProfile.InstructionsMaxLength} characters");

    public static Error NoPreferredMerchants(Guid productCategoryId) =>
        Error.Problem(
            "CollectionProfiles.NoPreferredMerchants",
            $"Neither the product category {productCategoryId} nor its ancestors define preferred merchants");
}
