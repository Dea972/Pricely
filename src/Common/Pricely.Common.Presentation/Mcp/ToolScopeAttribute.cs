namespace Pricely.Common.Presentation.Mcp;

/// <summary>
/// OAuth scope needed to approve a write tool (ADR-010, ADR-013), when it is not <c>pricely.write</c>
/// (for example <c>pricely.review</c> for a tool that confirms an offer).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ToolScopeAttribute(string scope) : Attribute
{
    public string Scope { get; } = scope;
}
