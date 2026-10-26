namespace Pricely.Modules.Catalog.Contracts.Categories;

/// <summary>
/// A product typology as Catalog exposes it to the other modules.
/// </summary>
/// <param name="Id">Identifier of the typology (the key other modules keep in a plain column, without foreign key).</param>
/// <param name="Name">Name of the typology.</param>
/// <param name="ParentId">Identifier of the parent typology; null for a root.</param>
/// <param name="Depth">Level in the tree, 1 for a root.</param>
public sealed record ProductCategorySummary(Guid Id, string Name, Guid? ParentId, int Depth);
