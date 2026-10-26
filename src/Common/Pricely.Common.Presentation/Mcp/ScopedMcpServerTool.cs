using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Pricely.Common.Presentation.Mcp;

/// <summary>
/// A tool of <c>/mcp</c> that checks the scope of the token at each call (ADR-016): the list a client sees is filtered
/// too, but a client can call a tool by name without listing it.
/// </summary>
internal sealed class ScopedMcpServerTool(McpServerTool innerTool, string requiredScope) : DelegatingMcpServerTool(innerTool)
{
    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default) =>
        McpServerExtensions.ScopesOf(request.Services).Contains(requiredScope)
            ? base.InvokeAsync(request, cancellationToken)
            : ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Le scope « {requiredScope} » est requis pour ce tool." }]
            });
}
