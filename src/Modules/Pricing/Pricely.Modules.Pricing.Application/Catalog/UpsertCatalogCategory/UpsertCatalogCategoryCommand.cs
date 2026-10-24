using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Catalog.UpsertCatalogCategory;

public sealed record UpsertCatalogCategoryCommand(Guid ProductCategoryId, string Name, Guid? ParentId) : ICommand;
