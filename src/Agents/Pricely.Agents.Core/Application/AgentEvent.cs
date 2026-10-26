using System.Text.Json;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// What a turn of an agent streams to the client (server-sent events). <see cref="Type"/> is the event name.
/// </summary>
public sealed record AgentEvent(string Type, object Data)
{
    public const string Conversation = "conversation";
    public const string ToolCall = "tool_call";
    public const string ToolResult = "tool_result";
    public const string ActionProposed = "action_proposed";
    public const string Text = "text";
    public const string Error = "error";
    public const string Done = "done";
}

/// <summary>Data of <see cref="AgentEvent.ToolCall"/>.</summary>
public sealed record ToolCallEvent(string CallId, string Name, bool IsReadOnly, IDictionary<string, object?>? Arguments);

/// <summary>Data of <see cref="AgentEvent.ToolResult"/>: <see cref="Result"/> is the tool's JSON, typed by tool name.</summary>
public sealed record ToolResultEvent(string CallId, string Name, JsonElement? Result, string? Error);

/// <summary>Data of <see cref="AgentEvent.ActionProposed"/>: a write tool call waiting for approval.</summary>
public sealed record ActionProposedEvent(Guid ActionId, string CallId, string ToolName, JsonElement Arguments, string RequiredScope);
