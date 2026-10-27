using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;
using Pricely.Modules.Pricing.Domain.Offers;

namespace Pricely.Modules.Pricing.Application.Offers.GetOffersPendingReview;

/// <summary>The offers come from Pricing, the identity of their products from Catalog (contract).</summary>
internal sealed class GetOffersPendingReviewQueryHandler(IOfferReadStore readStore, IProductReader productReader)
    : IQueryHandler<GetOffersPendingReviewQuery, IReadOnlyCollection<PendingOfferResponse>>
{
    public async Task<Result<IReadOnlyCollection<PendingOfferResponse>>> Handle(
        GetOffersPendingReviewQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PendingOfferRow> rows = await readStore.GetPendingReviewAsync(cancellationToken);

        Result<Dictionary<Guid, ProductSummary>> products =
            await ProductLookup.LoadAsync(productReader, rows.Select(r => r.ProductId), cancellationToken);
        if (products.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<PendingOfferResponse>>(products.Error);
        }

        // An offer of a product Catalog no longer knows is left out, as an inner join would.
        IReadOnlyCollection<PendingOfferResponse> offers =
        [
            .. rows
                .Where(r => products.Value.ContainsKey(r.ProductId))
                .Select(r => WithComparison(r, products.Value[r.ProductId]))
        ];

        return Result.Success(offers);
    }

    /// <summary>The comparison shown to the reviewer is the one of the matching policy (same normalization).</summary>
    private static PendingOfferResponse WithComparison(PendingOfferRow row, ProductSummary product)
    {
        MatchComparison comparison = OfferMatchingPolicy.Compare(
            new ProductIdentity(product.Gtin, product.Brand, product.ManufacturerReference),
            new MatchEvidence(row.ObservedGtin, row.ObservedBrand, row.ObservedManufacturerReference, row.AiSimilarity));

        return new PendingOfferResponse
        {
            OfferId = row.OfferId,
            ProductId = row.ProductId,
            ProductName = product.Name,
            MerchantId = row.MerchantId,
            MerchantName = row.MerchantName,
            ProductPage = row.ProductPage,
            MerchantProductReference = row.MerchantProductReference,
            AiSimilarity = row.AiSimilarity,
            ProposedOnUtc = row.ProposedOnUtc,
            LatestAmount = row.LatestAmount,
            LatestCurrency = row.LatestCurrency,
            ProductBrand = product.Brand,
            ProductGtin = product.Gtin,
            ProductManufacturerReference = product.ManufacturerReference,
            ObservedTitle = row.ObservedTitle,
            ObservedGtin = row.ObservedGtin,
            ObservedBrand = row.ObservedBrand,
            ObservedManufacturerReference = row.ObservedManufacturerReference,
            AiRationale = row.AiRationale,
            SameGtin = comparison.SameGtin,
            SameBrand = comparison.SameBrand,
            SameManufacturerReference = comparison.SameManufacturerReference
        };
    }
}
