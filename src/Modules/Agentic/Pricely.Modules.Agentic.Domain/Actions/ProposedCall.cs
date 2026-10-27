namespace Pricely.Modules.Agentic.Domain.Actions;

/// <param name="ToolName">The write tool the agent called.</param>
/// <param name="Arguments">Arguments of the call, as JSON.</param>
/// <param name="RequiredScope">OAuth scope the caller needs to approve it (ADR-010).</param>
public sealed record ProposedCall(string ToolName, string Arguments, string RequiredScope);
