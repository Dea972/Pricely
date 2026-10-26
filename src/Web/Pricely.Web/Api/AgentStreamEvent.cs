using System.Text.Json;

namespace Pricely.Web.Api;

/// <summary>
/// One server-sent event of a turn of an agent (<c>POST agents/{agent}/messages</c>), as the module Agentic names them.
/// </summary>
public abstract record AgentStreamEvent
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>The event, or null for an event type this front does not know (a newer API) or an unreadable one.</summary>
    public static AgentStreamEvent? Parse(string type, string data)
    {
        try
        {
            return Read(type, data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AgentStreamEvent? Read(string type, string data) =>
        type switch
        {
            AgentEventTypes.Conversation => JsonSerializer.Deserialize<ConversationStarted>(data, Options),
            AgentEventTypes.ToolCall => JsonSerializer.Deserialize<ToolCalled>(data, Options),
            AgentEventTypes.ToolResult => JsonSerializer.Deserialize<ToolResulted>(data, Options),
            AgentEventTypes.ActionProposed => JsonSerializer.Deserialize<ActionProposedEvent>(data, Options),
            AgentEventTypes.Text => JsonSerializer.Deserialize<TextReceived>(data, Options),
            AgentEventTypes.Error => JsonSerializer.Deserialize<TurnFailed>(data, Options),
            AgentEventTypes.Done => JsonSerializer.Deserialize<TurnDone>(data, Options),
            _ => null
        };
}

public sealed record ConversationStarted(Guid ConversationId, string Title) : AgentStreamEvent;

public sealed record ToolCalled(string CallId, string Name, bool IsReadOnly, JsonElement? Arguments) : AgentStreamEvent;

public sealed record ToolResulted(string CallId, string Name, JsonElement? Result, string? Error) : AgentStreamEvent;

public sealed record ActionProposedEvent(Guid ActionId, string CallId, string ToolName, JsonElement Arguments, string RequiredScope) : AgentStreamEvent;

public sealed record TextReceived(string Text) : AgentStreamEvent;

public sealed record TurnFailed(string Code, string Message) : AgentStreamEvent;

public sealed record TurnDone(Guid ConversationId) : AgentStreamEvent;

/// <summary>Names of the server-sent events of a turn.</summary>
public static class AgentEventTypes
{
    public const string Conversation = "conversation";
    public const string ToolCall = "tool_call";
    public const string ToolResult = "tool_result";
    public const string ActionProposed = "action_proposed";
    public const string Text = "text";
    public const string Error = "error";
    public const string Done = "done";
}
