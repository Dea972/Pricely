using FluentValidation;

namespace Pricely.Modules.Catalog.Application.Products.SearchProducts;

internal sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsQueryValidator()
    {
        RuleFor(q => q.Limit).InclusiveBetween(1, SearchProductsQuery.MaxLimit);
        RuleFor(q => q.Text).MaximumLength(200);
        RuleFor(q => q)
            .Must(q => !string.IsNullOrWhiteSpace(q.Text) || q.ProductCategoryId is not null)
            .WithMessage("Give a search text or a product category");
    }
}
