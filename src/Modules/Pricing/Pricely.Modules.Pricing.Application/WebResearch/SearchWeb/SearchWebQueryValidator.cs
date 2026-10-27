using FluentValidation;
using Pricely.Common.Application.Web;

namespace Pricely.Modules.Pricing.Application.WebResearch.SearchWeb;

internal sealed class SearchWebQueryValidator : AbstractValidator<SearchWebQuery>
{
    public SearchWebQueryValidator() =>
        RuleFor(q => q.Query).NotEmpty().MaximumLength(WebSearchRequest.MaxQueryLength);
}
