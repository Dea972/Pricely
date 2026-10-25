using FluentValidation;

namespace Pricely.Modules.AiEngine.Application.Models.SetModelPrice;

internal sealed class SetModelPriceCommandValidator : AbstractValidator<SetModelPriceCommand>
{
    public SetModelPriceCommandValidator()
    {
        RuleFor(c => c.ModelId).NotEmpty();
        RuleFor(c => c.InputPerMillion).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CachedInputPerMillion).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CacheWritePerMillion).GreaterThanOrEqualTo(0);
        RuleFor(c => c.OutputPerMillion).GreaterThanOrEqualTo(0);
    }
}
