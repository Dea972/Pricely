using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Catalog.Application.Products.GetProduct;

public sealed record GetProductQuery(Guid ProductId) : IQuery<ProductResponse>;
