namespace Pricely.Modules.Catalog.Application.Categories.GetProductCategories;

public sealed record ProductCategoryResponse(Guid Id, string Name, Guid? ParentId, int Depth);
