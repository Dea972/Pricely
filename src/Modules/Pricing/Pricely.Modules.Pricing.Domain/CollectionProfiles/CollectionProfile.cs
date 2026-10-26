using Pricely.Common.Domain;

namespace Pricely.Modules.Pricing.Domain.CollectionProfiles;

/// <summary>
/// How prices are collected for a product typology: which merchants to query first
/// and which instructions guide the collector. A typology without its own profile
/// inherits from its ancestors (see <see cref="CollectionProfileResolver"/>).
/// </summary>
public sealed class CollectionProfile : Entity
{
    public const int InstructionsMaxLength = 4000;

    private readonly List<PreferredMerchant> _preferredMerchants = [];

    private CollectionProfile()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductCategoryId { get; private set; }

    public string? Instructions { get; private set; }

    /// <summary>
    /// Preferred merchants ordered by priority (1 = queried first).
    /// </summary>
    public IReadOnlyList<PreferredMerchant> PreferredMerchants =>
        [.. _preferredMerchants.OrderBy(m => m.Priority)];

    public static Result<CollectionProfile> Define(
        Guid productCategoryId,
        IReadOnlyList<Guid> preferredMerchantIdsByPriority,
        string? instructions)
    {
        CollectionProfile profile = new()
        {
            Id = Guid.CreateVersion7(),
            ProductCategoryId = productCategoryId
        };

        Result result = profile.Update(preferredMerchantIdsByPriority, instructions);

        return result.IsSuccess ? profile : Result.Failure<CollectionProfile>(result.Error);
    }

    /// <summary>
    /// Replaces the preferred merchants (the list order defines the priority) and the instructions.
    /// An empty list means the typology inherits the preferred merchants of its parent.
    /// </summary>
    public Result Update(IReadOnlyList<Guid> preferredMerchantIdsByPriority, string? instructions)
    {
        if (preferredMerchantIdsByPriority.Distinct().Count() != preferredMerchantIdsByPriority.Count)
        {
            return Result.Failure(CollectionProfileErrors.DuplicatePreferredMerchant);
        }

        string? normalizedInstructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
        if (normalizedInstructions?.Length > InstructionsMaxLength)
        {
            return Result.Failure(CollectionProfileErrors.InstructionsTooLong);
        }

        ReplacePreferredMerchants(preferredMerchantIdsByPriority);
        Instructions = normalizedInstructions;

        return Result.Success();
    }

    /// <summary>
    /// Adds a merchant last by priority. A profile without merchants of its own inherits them: it starts from the
    /// <paramref name="inheritedMerchantIds"/>, so that adding one does not drop the others.
    /// </summary>
    public Result AddPreferredMerchant(Guid merchantId, IReadOnlyList<Guid> inheritedMerchantIds)
    {
        List<Guid> merchants = _preferredMerchants.Count > 0 ? [.. PreferredMerchants.Select(m => m.MerchantId)] : [.. inheritedMerchantIds];
        if (!merchants.Contains(merchantId))
        {
            merchants.Add(merchantId);
        }

        return Update(merchants, Instructions);
    }

    private void ReplacePreferredMerchants(IReadOnlyList<Guid> merchantIdsByPriority)
    {
        _preferredMerchants.RemoveAll(existing => !merchantIdsByPriority.Contains(existing.MerchantId));

        for (int index = 0; index < merchantIdsByPriority.Count; index++)
        {
            Guid merchantId = merchantIdsByPriority[index];
            int priority = index + 1;

            PreferredMerchant? existing = _preferredMerchants.Find(m => m.MerchantId == merchantId);
            if (existing is null)
            {
                _preferredMerchants.Add(new PreferredMerchant(merchantId, priority));
            }
            else
            {
                existing.ChangePriority(priority);
            }
        }
    }
}
