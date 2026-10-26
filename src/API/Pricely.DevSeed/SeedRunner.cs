using MediatR;
using Pricely.Common.Application.Clock;
using Pricely.Common.Domain;
using Pricely.Modules.AiEngine.Application.Models.GetModels;
using Pricely.Modules.AiEngine.Application.Models.RegisterModel;
using Pricely.Modules.Catalog.Application.Categories.CreateProductCategory;
using Pricely.Modules.Catalog.Application.Categories.GetProductCategories;
using Pricely.Modules.Catalog.Application.Products.GetProduct;
using Pricely.Modules.Catalog.Application.Products.RegisterProduct;
using Pricely.Modules.Catalog.Application.Products.SearchProducts;
using Pricely.Modules.Pricing.Application.CollectionProfiles.DefineCollectionProfile;
using Pricely.Modules.Pricing.Application.Merchants.DeactivateMerchant;
using Pricely.Modules.Pricing.Application.Merchants.GetMerchants;
using Pricely.Modules.Pricing.Application.Merchants.RegisterMerchant;
using Pricely.Modules.Pricing.Application.Offers.CollectProductOffers;
using Pricely.Modules.Pricing.Application.Offers.GetProductOffers;

namespace Pricely.DevSeed;

/// <summary>
/// Writes the test data through the commands of the modules, step by step, and leaves alone what already exists (same
/// name, same GTIN, a product that already has offers): it can be run again.
/// </summary>
internal sealed class SeedRunner(
    IServiceScopeFactory scopeFactory,
    SeedOfferCollector collector,
    IDateTimeProvider clock,
    IConfiguration configuration,
    ILogger<SeedRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, Guid> categories = await CategoriesAsync(cancellationToken);
        Dictionary<string, Guid> products = await ProductsAsync(categories, cancellationToken);
        Dictionary<string, Guid> merchants = await MerchantsAsync(cancellationToken);

        await ProfilesAsync(categories, merchants, cancellationToken);
        await ObservationsAsync(products, cancellationToken);
        await ModelAsync(cancellationToken);

        logger.LogInformation(
            "Test data ready: {Categories} typologies, {Products} products, {Merchants} merchants",
            categories.Count,
            products.Count,
            merchants.Count);
    }

    private async Task<Dictionary<string, Guid>> CategoriesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProductCategoryResponse> existing = await SendAsync(new GetProductCategoriesQuery(), cancellationToken);
        var ids = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (SeedCategory category in SeedData.Categories)
        {
            Guid? parentId = category.ParentKey is null ? null : ids[category.ParentKey];
            ProductCategoryResponse? known = existing.FirstOrDefault(c =>
                c.ParentId == parentId && string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase));

            ids[category.Key] = known?.Id ?? await SendAsync(new CreateProductCategoryCommand(category.Name, parentId), cancellationToken);
        }

        return ids;
    }

    private async Task<Dictionary<string, Guid>> ProductsAsync(Dictionary<string, Guid> categories, CancellationToken cancellationToken)
    {
        var ids = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (SeedProduct product in SeedData.Products)
        {
            IReadOnlyCollection<ProductResponse> found = await SendAsync(new SearchProductsQuery(product.Gtin, null, 5), cancellationToken);
            ProductResponse? known = found.FirstOrDefault(p => string.Equals(p.Gtin, product.Gtin, StringComparison.Ordinal));

            ids[product.Gtin] = known?.Id ?? await SendAsync(
                new RegisterProductCommand(product.Name, product.Brand, product.Gtin, product.Reference, categories[product.CategoryKey]),
                cancellationToken);
        }

        return ids;
    }

    private async Task<Dictionary<string, Guid>> MerchantsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MerchantResponse> existing = await SendAsync(new GetMerchantsQuery(), cancellationToken);
        var ids = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (SeedMerchant merchant in SeedData.Merchants)
        {
            MerchantResponse? known = existing.FirstOrDefault(m => string.Equals(m.Name, merchant.Name, StringComparison.OrdinalIgnoreCase));
            if (known is not null)
            {
                ids[merchant.Key] = known.Id;
                continue;
            }

            Guid id = await SendAsync(new RegisterMerchantCommand(merchant.Name, merchant.Website, merchant.SearchTemplate), cancellationToken);
            ids[merchant.Key] = id;

            if (!merchant.IsActive)
            {
                await SendAsync(new DeactivateMerchantCommand(id), cancellationToken);
            }
        }

        return ids;
    }

    private async Task ProfilesAsync(Dictionary<string, Guid> categories, Dictionary<string, Guid> merchants, CancellationToken cancellationToken)
    {
        foreach (SeedProfile profile in SeedData.Profiles)
        {
            await SendAsync(
                new DefineCollectionProfileCommand(
                    categories[profile.CategoryKey],
                    [.. profile.MerchantKeys.Select(key => merchants[key])],
                    profile.Instructions),
                cancellationToken);
        }
    }

    /// <summary>One collection a week for each product, the oldest first, so that the price guard sees the history.</summary>
    private async Task ObservationsAsync(Dictionary<string, Guid> products, CancellationToken cancellationToken)
    {
        List<Guid> toCollect = [];
        foreach (Guid productId in products.Values)
        {
            ProductOffersResponse offers = await SendAsync(new GetProductOffersQuery(productId), cancellationToken);
            if (offers.Offers.Count == 0)
            {
                toCollect.Add(productId);
            }
        }

        if (toCollect.Count == 0)
        {
            logger.LogInformation("Every product already has offers: no observation added");

            return;
        }

        DateTime today = clock.UtcNow.Date.AddHours(8);
        for (int week = 0; week < SeedData.Weeks; week++)
        {
            collector.Week = week;
            collector.ObservedOnUtc = today.AddDays(-7 * (SeedData.Weeks - 1 - week));

            foreach (Guid productId in toCollect)
            {
                await SendAsync(new CollectProductOffersCommand(productId), cancellationToken);
            }
        }

        logger.LogInformation("{Weeks} weeks of observations for {Products} products", SeedData.Weeks, toCollect.Count);
    }

    /// <summary>The default model in the catalogue, without price: its price is entered in « IA et coûts ».</summary>
    private async Task ModelAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ModelResponse> models = await SendAsync(new GetModelsQuery(), cancellationToken);
        string? defaultModel = configuration["Ai:Anthropic:DefaultModel"];

        if (models.Count == 0 && !string.IsNullOrWhiteSpace(defaultModel))
        {
            await SendAsync(new RegisterModelCommand("Anthropic", defaultModel, defaultModel), cancellationToken);
        }
    }

    private async Task<T> SendAsync<T>(IRequest<Result<T>> request, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        Result<T> result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);

        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException($"{request.GetType().Name} failed: {result.Error.Code} {result.Error.Description}");
    }

    private async Task SendAsync(IRequest<Result> request, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        Result result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"{request.GetType().Name} failed: {result.Error.Code} {result.Error.Description}");
        }
    }
}
