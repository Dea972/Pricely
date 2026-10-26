namespace Pricely.Modules.Agentic.Application;

/// <summary>
/// Settings of the Agentic module (section <c>Agentic</c>).
/// </summary>
public sealed class AgenticOptions
{
    public const string SectionName = "Agentic";

    /// <summary>OAuth scope needed to approve a write tool that declares none (<c>pricely.write</c> in Pricely).</summary>
    public string DefaultApprovalScope { get; init; } = "write";

    /// <summary>Maximum number of model calls in one turn (tool round-trips included).</summary>
    public int MaximumToolRoundTrips { get; init; } = 10;
}
