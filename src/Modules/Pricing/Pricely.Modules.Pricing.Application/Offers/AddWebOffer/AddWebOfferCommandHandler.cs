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
using Pricely.Modules.Pricing.Domain.Merchants;
using Pricely.Modules.Pricing.Domain.Offers;

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

        Result<OfferCollectionPlan> plan = await offerCollectionPlanner.PlanAsync(found.Id, cancellationToken);
        bool addedToProfile = !plan.IsSuccess || plan.Value.Request.Merchants.All(m => m.MerchantId != merchant.Id);
        if (addedToProfile)
        {
            Result added = await sender.Send(new AddMerchantToProfilesCommand(merchant.Id, [found.ProductCategoryId]), cancellationToken);
            if (added.IsFailure)
            {
                return Result.Failure<AddedWebOfferResponse>(added.Error);
            }
        }

        Result<SubmittedOfferResponse> submitted = await sender.Send(
            new SubmitCollectedOfferCommand(found.Id, Offer(page.Value, found, request)),
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
            request.Price,
            request.Currency.Trim().ToUpperInvariant(),
            submitted.Value.Outcome,
            submitted.Value.IsFlaggedAsAnomaly,
            addedToProfile);
    }

    /// <summary>
    /// What the page states about the offer of this price, from its structured data when it declares it. The identifiers
    /// are those of the reference product: the verifier keeps only the ones the page shows.
    /// </summary>
    private static SubmittedOffer Offer(MerchantPage page, ProductSummary product, AddWebOfferCommand request)
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
            ObservedGtin = product.Gtin,
            ObservedBrand = product.Brand,
            ObservedManufacturerReference = product.ManufacturerReference,
            Rationale = Rationale
        };
    }
}
