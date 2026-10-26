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
        RuleFor(c => c.Source)
            .Must(source => source is null || IsKnown(source))
            .WithMessage("The source is Manual, LiteLlm or AiReading.");
    }

    /// <summary>A name of <see cref="Domain.Models.PriceSource"/>, not a number.</summary>
    private static bool IsKnown(string source) =>
        !int.TryParse(source, out _) && Enum.TryParse(source, ignoreCase: true, out Domain.Models.PriceSource parsed) && Enum.IsDefined(parsed);
}
