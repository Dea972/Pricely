using MediatR;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Application.Web;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Contracts.Products;
using Pricely.Modules.Pricing.Application.Merchants.AddMerchantToProfiles;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Application.Offers.SubmitCollectedOffer;
using Pricely.Modules.Pricing.Application.PageSnapshots;
using Pricely.Modules.Pricing.Domain.Catalog;
using Pricely.Modules.Pricing.Domain.CollectionProfiles;
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;
using Pricely.Modules.Pricing.Domain.PageSnapshots;

namespace Pricely.Modules.Pricing.Application.Offers.AddWebOffer;

/// <summary>
/// Composes the existing use cases, each with its own save (one aggregate per transaction): the merchant joins the
/// typology's profile if needed (<see cref="AddMerchantToProfilesCommand"/>), then the offer read on the fresh snapshot
/// goes through <see cref="SubmitCollectedOfferCommand"/> (price on the page, matching, price guard).
/// </summary>
internal sealed class AddWebOfferCommandHandler(
    IProductReader productReader,
    IMerchantRepository merchantRepository,
    MerchantPageReader pageReader,
    OfferCollectionPlanner offerCollectionPlanner,
    ISender sender)
    : ICommandHandler<AddWebOfferCommand, AddedWebOfferResponse>
{
    private static readonly string NoPreferredMerchantsCode = CollectionProfileErrors.NoPreferredMerchants(Guid.Empty).Code;

    public const string Rationale = "Offre trouvée par une recherche web de l'assistant, page relue à la validation du lot.";

    public async Task<Result<AddedWebOfferResponse>> Handle(AddWebOfferCommand request, CancellationToken cancellationToken)
    {
        Result<ProductSummary?> product = await productReader.FindAsync(request.ProductGtin, request.ProductBrand, request.ProductReference, cancellationToken);
        if (product.IsFailure)
        {
            return Result.Failure<AddedWebOfferResponse>(product.Error);
        }

        if (product.Value is not { } found)
        {
            return Result.Failure<AddedWebOfferResponse>(CatalogErrors.ProductNotFoundByKey(request.ProductGtin, request.ProductBrand, request.ProductReference));
        }

        IReadOnlyCollection<Merchant> merchants = await merchantRepository.GetAllAsync(cancellationToken);
        Merchant? merchant = merchants.FirstOrDefault(m => m.Owns(request.PageAddress));
        if (merchant is null)
        {
            return Result.Failure<AddedWebOfferResponse>(MerchantErrors.NoMerchantForSite(request.PageAddress));
        }

        // Read again now: the page may have changed since the model read it.
        Result<MerchantPage> page = await pageReader.ReadPageAsync(merchant.Id, request.PageAddress, null, cancellationToken);
        if (page.IsFailure)
        {
            return Result.Failure<AddedWebOfferResponse>(page.Error);
        }

        // Nothing changes (not even the profile) for a page that no longer shows the price.
        PageSnapshot snapshot = page.Value.Snapshot;
        if (snapshot.IsSearchPage)
        {
            return Result.Failure<AddedWebOfferResponse>(PageSnapshotErrors.SearchPage(snapshot.Id));
        }

        if (!snapshot.ShowsPrice(request.Price))
        {
            return Result.Failure<AddedWebOfferResponse>(PageSnapshotErrors.PriceNotShown(request.Price));
        }

        Result<bool> inPlan = await IsInPlanAsync(found.Id, merchant.Id, cancellationToken);
        if (inPlan.IsFailure)
        {
            return Result.Failure<AddedWebOfferResponse>(inPlan.Error);
        }

        bool addedToProfile = !inPlan.Value;
        if (addedToProfile)
        {
            Result added = await sender.Send(new AddMerchantToProfilesCommand(merchant.Id, [found.ProductCategoryId]), cancellationToken);
            if (added.IsFailure)
            {
                return Result.Failure<AddedWebOfferResponse>(added.Error);
            }
        }

        SubmittedOffer offer = Offer(page.Value, request);
        Result<SubmittedOfferResponse> submitted = await sender.Send(
            new SubmitCollectedOfferCommand(found.Id, offer),
            cancellationToken);
        if (submitted.IsFailure)
        {
            return Result.Failure<AddedWebOfferResponse>(submitted.Error);
        }

        return new AddedWebOfferResponse(
            found.Id,
            found.Name,
            merchant.Id,
            merchant.Name,
            page.Value.Snapshot.Address,
            offer.Price,
            offer.Currency.Trim().ToUpperInvariant(),
            submitted.Value.Outcome,
            submitted.Value.IsFlaggedAsAnomaly,
            addedToProfile);
    }

    /// <summary>
    /// Whether the merchant is already searched for the product. A typology whose profile has no merchant yet is not an
    /// error here: the merchant will be its first one.
    /// </summary>
    private async Task<Result<bool>> IsInPlanAsync(Guid productId, Guid merchantId, CancellationToken cancellationToken)
    {
        Result<OfferCollectionPlan> plan = await offerCollectionPlanner.PlanAsync(productId, cancellationToken);
        if (plan.IsSuccess)
        {
            return plan.Value.Request.Merchants.Any(m => m.MerchantId == merchantId);
        }

        if (plan.Error.Code == NoPreferredMerchantsCode)
        {
            return Result.Success(false);
        }

        return Result.Failure<bool>(plan.Error);
    }

    /// <summary>
    /// What the page itself declares (JSON-LD) about the offer of this price. Only those identifiers are given to the
    /// matching, never the reference product's: an identifier written elsewhere on the page (a similar product, a lot)
    /// must not confirm the offer (ADR-019). The manufacturer reference is never declared there (the SKU is the
    /// merchant's own): without a declared GTIN, the offer waits for a review.
    /// </summary>
    private static SubmittedOffer Offer(MerchantPage page, AddWebOfferCommand request)
    {
        (StructuredProduct Product, StructuredOffer Offer)? declared = page.Products
            .SelectMany(p => p.Offers.Select(o => (Product: p, Offer: o)))
            .Where(d => d.Offer.Price == request.Price)
            .Select(d => ((StructuredProduct Product, StructuredOffer Offer)?)d)
            .FirstOrDefault();

        return new SubmittedOffer
        {
            SnapshotId = page.Snapshot.Id,
            Price = request.Price,
            Currency = declared?.Offer.Currency ?? request.Currency,
            Availability = declared?.Offer.Availability switch
            {
                "InStock" or "InStoreOnly" or "OnlineOnly" or "LimitedAvailability" => Availability.InStock,
                "OutOfStock" or "SoldOut" or "Discontinued" => Availability.OutOfStock,
                _ => Availability.Unknown
            },
            MerchantProductReference = declared?.Product.Sku,
            ObservedTitle = declared?.Product.Name,
            ObservedGtin = declared?.Product.Gtin,
            ObservedBrand = declared?.Product.Brand,
            Rationale = Rationale
        };
    }
}
