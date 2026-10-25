using Pricely.Common.Domain;

namespace Pricely.Modules.AiEngine.Domain.Models;

/// <summary>
/// An LLM as called at a provider, with the history of its prices. A call is linked to the model whose provider and
/// identifier match the ones it reports.
/// </summary>
public sealed class Model : Entity
{
    public const int ProviderModelIdMaxLength = 150;

    public const int LabelMaxLength = 150;

    private readonly List<ModelPrice> _prices = [];

    private Model()
    {
    }

    public Guid Id { get; private set; }

    public Provider Provider { get; private set; }

    /// <summary>Identifier of the model at the provider, as reported by calls (for example <c>claude-sonnet-5-5</c>). Unique per provider.</summary>
    public string ProviderModelId { get; private set; }

    public string Label { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public IReadOnlyCollection<ModelPrice> Prices => [.. _prices];

    public static Result<Model> Create(Provider provider, string providerModelId, string label, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(providerModelId) || providerModelId.Trim().Length > ProviderModelIdMaxLength)
        {
            return Result.Failure<Model>(ModelErrors.InvalidProviderModelId);
        }

        if (string.IsNullOrWhiteSpace(label) || label.Trim().Length > LabelMaxLength)
        {
            return Result.Failure<Model>(ModelErrors.InvalidLabel);
        }

        return new Model
        {
            Id = Guid.CreateVersion7(),
            Provider = provider,
            ProviderModelId = providerModelId.Trim(),
            Label = label.Trim(),
            CreatedOnUtc = utcNow
        };
    }

    /// <summary>
    /// Sets the price applicable from <paramref name="validFrom"/>; replaces the price already set on that day.
    /// </summary>
    public Result SetPrice(DateOnly validFrom, PriceValues values, PriceSource source, DateTime utcNow)
    {
        if (values.InputPerMillion < 0 || values.CachedInputPerMillion < 0 ||
            values.CacheWritePerMillion < 0 || values.OutputPerMillion < 0)
        {
            return Result.Failure(ModelErrors.NegativePrice);
        }

        ModelPrice? existing = _prices.Find(p => p.ValidFrom == validFrom);
        if (existing is null)
        {
            _prices.Add(ModelPrice.Create(Id, validFrom, values, source, utcNow));
        }
        else
        {
            existing.Update(values, source, utcNow);
        }

        return Result.Success();
    }

    /// <summary>Price in force on the day of a call (UTC); null if no price was set yet.</summary>
    public ModelPrice? PriceAt(DateTime calledOnUtc)
    {
        var day = DateOnly.FromDateTime(calledOnUtc);

        return _prices.Where(p => p.ValidFrom <= day).MaxBy(p => p.ValidFrom);
    }
}
