using FluentValidation;
using Pricely.Modules.AiEngine.Domain.Models;

namespace Pricely.Modules.AiEngine.Application.Models.RegisterModel;

internal sealed class RegisterModelCommandValidator : AbstractValidator<RegisterModelCommand>
{
    public RegisterModelCommandValidator()
    {
        RuleFor(c => c.Provider).NotEmpty();
        RuleFor(c => c.ProviderModelId).NotEmpty().MaximumLength(Model.ProviderModelIdMaxLength);
        RuleFor(c => c.Label).NotEmpty().MaximumLength(Model.LabelMaxLength);
    }
}
