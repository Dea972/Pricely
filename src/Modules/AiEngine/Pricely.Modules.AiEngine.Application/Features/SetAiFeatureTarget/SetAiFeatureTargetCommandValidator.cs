using FluentValidation;
using Pricely.Modules.AiEngine.Domain.Features;

namespace Pricely.Modules.AiEngine.Application.Features.SetAiFeatureTarget;

internal sealed class SetAiFeatureTargetCommandValidator : AbstractValidator<SetAiFeatureTargetCommand>
{
    public SetAiFeatureTargetCommandValidator()
    {
        RuleFor(c => c.FeatureCode).NotEmpty().MaximumLength(FeatureSetting.FeatureCodeMaxLength);
        RuleFor(c => c.ModelId).NotEmpty();
    }
}
