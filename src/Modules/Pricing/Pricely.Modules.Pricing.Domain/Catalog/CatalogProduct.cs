namespace Pricely.Modules.Pricing.Domain.Catalog;

/// <summary>
/// Local read model of a Catalog product, fed by Catalog integration events (idempotent upsert on <see cref="Id"/>).
/// </summary>
public sealed class CatalogProduct
{
    private CatalogProduct()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Brand { get; private set; }

    public string? Gtin { get; private set; }

    public string? ManufacturerReference { get; private set; }

    public Guid ProductCategoryId { get; private set; }

    public static CatalogProduct Create(Guid id, CatalogProductSnapshot snapshot)
    {
        CatalogProduct product = new() { Id = id };
        product.Update(snapshot);

        return product;
    }

    public void Update(CatalogProductSnapshot snapshot)
    {
        Name = snapshot.Name;
        Brand = snapshot.Brand;
        Gtin = snapshot.Gtin;
        ManufacturerReference = snapshot.ManufacturerReference;
        ProductCategoryId = snapshot.ProductCategoryId;
    }
}
