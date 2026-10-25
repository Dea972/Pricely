using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Pricing.Application.Catalog.RenameCatalogCategory;

public sealed record RenameCatalogCategoryCommand(Guid ProductCategoryId, string Name) : ICommand;
