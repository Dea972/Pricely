namespace Pricely.Modules.Pricing.Domain.Catalog;

/// <summary>
/// Local read model of a Catalog product category (typology), fed by Catalog integration events.
/// </summary>
public sealed class CatalogCategory
{
    private CatalogCategory()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid? ParentId { get; private set; }

    public static CatalogCategory Create(Guid id, string name, Guid? parentId) =>
        new() { Id = id, Name = name, ParentId = parentId };

    public void Rename(string name) => Name = name;
}
