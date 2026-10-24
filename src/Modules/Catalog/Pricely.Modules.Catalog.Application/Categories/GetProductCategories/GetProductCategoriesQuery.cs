using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Categories.GetProductCategories;

public sealed record GetProductCategoriesQuery : IQuery<IReadOnlyCollection<ProductCategoryResponse>>;
