using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Catalog.Application.Abstractions.Data;
using Pricely.Modules.Catalog.Domain.Categories;
using Pricely.Modules.Catalog.Domain.Products;

namespace Pricely.Modules.Catalog.Application.Products.FindOrRegisterProduct;

/// <summary>
/// Finds the product by GTIN (any length), then by brand and manufacturer reference; otherwise finds or creates the
/// typology path and registers the product, in one save.
/// </summary>
internal sealed class FindOrRegisterProductCommandHandler(
    IProductRepository productRepository,
    IProductCategoryRepository productCategoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<FindOrRegisterProductCommand, FoundProductResponse>
{
    public async Task<Result<FoundProductResponse>> Handle(FindOrRegisterProductCommand request, CancellationToken cancellationToken)
    {
        Result<ProductDescription> description = ProductDescriptionFactory.Create(
            request.Name,
            request.Brand,
            request.Gtin,
            request.ManufacturerReference);
        if (description.IsFailure)
        {
            return Result.Failure<FoundProductResponse>(description.Error);
        }

        Product? existing = await FindAsync(description.Value, cancellationToken);
        if (existing is not null)
        {
            return new FoundProductResponse(existing.Id, existing.Name, existing.Brand, existing.Gtin?.Value, existing.ProductCategoryId, false, []);
        }

        var created = new List<ProductCategory>();
        Result<ProductCategory> category = await FindOrCreatePathAsync(request.CategoryPath, created, cancellationToken);
        if (category.IsFailure)
        {
            return Result.Failure<FoundProductResponse>(category.Error);
        }

        Result<Product> product = Product.Register(description.Value, category.Value.Id);
        if (product.IsFailure)
        {
            return Result.Failure<FoundProductResponse>(product.Error);
        }

        foreach (ProductCategory newCategory in created)
        {
            productCategoryRepository.Insert(newCategory);
        }

        productRepository.Insert(product.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FoundProductResponse(
            product.Value.Id,
            product.Value.Name,
            product.Value.Brand,
            product.Value.Gtin?.Value,
            category.Value.Id,
            true,
            [.. created.Select(c => c.Name)]);
    }

    private async Task<Product?> FindAsync(ProductDescription description, CancellationToken cancellationToken)
    {
        if (description.Gtin is not null)
        {
            Product? byGtin = await productRepository.FindByEquivalentGtinAsync(description.Gtin, cancellationToken);
            if (byGtin is not null)
            {
                return byGtin;
            }
        }

        if (string.IsNullOrWhiteSpace(description.ManufacturerReference))
        {
            return null;
        }

        IReadOnlyList<Product> sameBrand = await productRepository.GetByBrandAsync(description.Brand, cancellationToken);

        // A product of another GTIN is another product, even with the same reference (another packaging, ADR-019).
        return sameBrand.FirstOrDefault(p =>
            p.HasManufacturerReference(description.ManufacturerReference)
            && (description.Gtin is null || p.Gtin is null));
    }

    /// <summary>The typology of the path, each level found by name (case ignored) under its parent, else created.</summary>
    private async Task<Result<ProductCategory>> FindOrCreatePathAsync(
        IReadOnlyList<string> path,
        List<ProductCategory> created,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProductCategory> categories = await productCategoryRepository.GetAllAsync(cancellationToken);
        ProductCategory? parent = null;

        foreach (string level in path.Select(name => name.Trim()))
        {
            // Under a typology created just now, nothing exists yet.
            ProductCategory? found = parent is not null && created.Contains(parent)
                ? null
                : categories.FirstOrDefault(c => c.ParentId == parent?.Id && string.Equals(c.Name, level, StringComparison.OrdinalIgnoreCase));

            if (found is null)
            {
                Result<ProductCategory> child = parent is null ? ProductCategory.CreateRoot(level) : ProductCategory.CreateChild(level, parent);
                if (child.IsFailure)
                {
                    return child;
                }

                found = child.Value;
                created.Add(found);
            }

            parent = found;
        }

        return parent!;
    }
}
