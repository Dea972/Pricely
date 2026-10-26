namespace Pricely.Modules.Agentic.Application;

/// <summary>
/// Settings of the Agentic module (section <c>Agentic</c>).
/// </summary>
public sealed class AgenticOptions
{
    public const string SectionName = "Agentic";

    /// <summary>
    /// OAuth scope needed to approve a write tool that declares none (<c>pricely.write</c> in Pricely). The default,
    /// <c>write</c>, is deliberately a scope nobody has until the application sets it: no write tool is approvable by mistake.
    /// </summary>
    public string DefaultApprovalScope { get; init; } = "write";

    /// <summary>
    /// Authorization policy of the module's endpoints (<c>pricely.read</c> in Pricely); any authenticated caller when null.
    /// </summary>
    public string? AccessPolicy { get; init; }

    /// <summary>Maximum number of model calls in one turn (tool round-trips included).</summary>
    public int MaximumToolRoundTrips { get; init; } = 10;
}
