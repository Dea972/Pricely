using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Categories.RenameProductCategory;

public sealed record RenameProductCategoryCommand(Guid ProductCategoryId, string Name) : ICommand;
