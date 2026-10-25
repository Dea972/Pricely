using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Models;

public static class ModelErrors
{
    public static readonly Error InvalidProviderModelId = Error.Problem(
        "Models.InvalidProviderModelId",
        $"The provider model identifier is required and cannot exceed {Model.ProviderModelIdMaxLength} characters");

    public static readonly Error InvalidLabel = Error.Problem(
        "Models.InvalidLabel",
        $"The label is required and cannot exceed {Model.LabelMaxLength} characters");

    public static readonly Error NegativePrice = Error.Problem("Models.NegativePrice", "A price cannot be negative");

    public static readonly Error AlreadyExists = Error.Conflict(
        "Models.AlreadyExists",
        "A model with this identifier already exists for this provider");

    public static Error NotFound(Guid modelId) =>
        Error.NotFound("Models.NotFound", $"The model with the identifier {modelId} was not found");

    public static Error UnknownProvider(string provider) =>
        Error.Problem("Models.UnknownProvider", $"'{provider}' is not a supported provider");
}
