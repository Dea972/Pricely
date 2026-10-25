using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Categories.CreateProductCategory;

public sealed record CreateProductCategoryCommand(string Name, Guid? ParentId) : ICommand<Guid>;
