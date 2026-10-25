using Pricely.Common.Application.Messaging;
using Pricely.Modules.Pricing.Domain.Catalog;

namespace Pricely.Modules.Pricing.Application.Catalog.UpsertCatalogProduct;

public sealed record UpsertCatalogProductCommand(Guid ProductId, CatalogProductSnapshot Snapshot) : ICommand;
