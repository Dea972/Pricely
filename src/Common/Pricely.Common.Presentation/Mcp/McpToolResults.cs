using ModelContextProtocol;
using Pricely.Common.Domain;

namespace Pricely.Common.Presentation.Mcp;

public static class McpToolResults
{
    /// <summary>
    /// Value of a successful result; otherwise an MCP tool error carrying the error code and description,
    /// so that the agent can understand and recover.
    /// </summary>
    public static T ValueOrToolError<T>(this Result<T> result) =>
        result.IsSuccess ? result.Value : throw new McpException($"{result.Error.Code}: {result.Error.Description}");

    /// <summary>Nothing on success; otherwise the same MCP tool error as <see cref="ValueOrToolError{T}"/>.</summary>
    public static void ThrowIfToolError(this Result result)
    {
        if (result.IsFailure)
        {
            throw new McpException($"{result.Error.Code}: {result.Error.Description}");
        }
    }
}
