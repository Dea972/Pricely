using System.Text;
using System.Text.Json;
using Pricely.Web.Api;

namespace Pricely.Web.Assistant;

/// <summary>
/// What the assistant screen shows of a conversation: the user's messages and, for each of them, the answer of the agent
/// (tools used, their typed results, proposed actions, text). Built from the events of a turn as they arrive, or from
/// the stored conversation. Display only: every rule stays in the API.
/// </summary>
public sealed class ConversationView
{
    private const string UserRole = "User";
    private const string ToolRole = "Tool";

    private readonly List<ChatEntry> _entries = [];

    public Guid? ConversationId { get; private set; }

    public string? Title { get; private set; }

    public IReadOnlyList<ChatEntry> Entries => _entries;

    public bool IsRunning => _entries.Count > 0 && _entries[^1] is AgentEntry { IsComplete: false };

    /// <summary>The tools of the whole conversation, in the order of their first use.</summary>
    public IReadOnlyList<ToolUse> ToolsUsed =>
        _entries.OfType<AgentEntry>().SelectMany(a => a.Tools).DistinctBy(t => t.Name, StringComparer.Ordinal).ToList();

    /// <summary>The product the conversation talks about last: the latest tool call about a product.</summary>
    public Guid? CurrentProductId =>
        _entries.OfType<AgentEntry>()
            .SelectMany(a => a.Tools)
            .Select(t => ToolCatalog.ProductIdOf(t.Arguments))
            .LastOrDefault(id => id is not null);

    /// <summary>Starts a turn: the user's message and the answer that the events will fill.</summary>
    public AgentEntry Send(string message)
    {
        _entries.Add(new UserEntry(message));
        var answer = new AgentEntry();
        _entries.Add(answer);

        return answer;
    }

    /// <summary>Applies an event of the running turn to its answer.</summary>
    public void Apply(AgentStreamEvent agentEvent)
    {
        AgentEntry answer = _entries.Count > 0 && _entries[^1] is AgentEntry last
            ? last
            : throw new InvalidOperationException("No turn is running.");

        switch (agentEvent)
        {
            case ConversationStarted started:
                ConversationId = started.ConversationId;
                Title = started.Title;
                break;
            case ToolCalled call:
                answer.AddTool(new ToolUse(call.CallId, call.Name, call.IsReadOnly, call.Arguments));
                break;
            case ToolResulted result:
                answer.CompleteTool(result.CallId, result.Name, result.Result, result.Error);
                break;
            case ActionProposedEvent proposed:
                answer.Propose(new ProposedAction(proposed.ActionId, proposed.ToolName, proposed.Arguments, proposed.RequiredScope) { BatchId = proposed.BatchId });
                answer.MarkAwaitingApproval(proposed.CallId);
                break;
            case TextReceived text:
                answer.AppendText(text.Text);
                break;
            case TurnFailed failed:
                answer.Fail(failed.Message);
                break;
            case TurnDone done:
                ConversationId = done.ConversationId;
                answer.Complete();
                break;
            default:
                break;
        }
    }

    /// <summary>Ends the running turn when the stream stops without <c>done</c> (network, expired session).</summary>
    public void Interrupt(string message)
    {
        if (IsRunning && _entries[^1] is AgentEntry answer)
        {
            // An error sent by the API before it closed the stream says more than the interruption.
            answer.Fail(answer.Error ?? message);
            answer.Complete();
        }
    }

    /// <summary>A stored conversation: consecutive messages of the model and of its tools form one answer.</summary>
    public static ConversationView FromHistory(ConversationDto conversation)
    {
        var view = new ConversationView { ConversationId = conversation.Id, Title = conversation.Title };
        var actions = conversation.Actions.ToDictionary(a => a.Id);
        AgentEntry? answer = null;

        foreach (ConversationMessageDto message in conversation.Messages.OrderBy(m => m.Sequence))
        {
            if (string.Equals(message.Role, UserRole, StringComparison.Ordinal))
            {
                answer?.Complete();
                answer = null;
                view._entries.Add(new UserEntry(message.Text));
                continue;
            }

            if (answer is null)
            {
                answer = new AgentEntry();
                view._entries.Add(answer);
            }

            foreach (ToolCallDto call in message.ToolCalls)
            {
                answer.AddTool(new ToolUse(call.CallId, call.Name, call.IsReadOnly, call.Arguments));
            }

            foreach (ToolResultDto result in message.ToolResults)
            {
                if (ProposedActionId(result.Result) is Guid actionId && actions.TryGetValue(actionId, out PendingActionDto? action))
                {
                    answer.Propose(ProposedAction.From(action));
                    answer.MarkAwaitingApproval(result.CallId);
                }
                else
                {
                    answer.CompleteTool(result.CallId, result.Name, result.Result, result.Error);
                }
            }

            if (!string.Equals(message.Role, ToolRole, StringComparison.Ordinal))
            {
                answer.AppendText(message.Text);
            }
        }

        answer?.Complete();

        return view;
    }

    /// <summary>The result that the module gives the model for a write tool: <c>{ status: "pending_approval", actionId }</c>.</summary>
    private static Guid? ProposedActionId(JsonElement? result) =>
        result is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty("status", out JsonElement status)
            && status.ValueKind == JsonValueKind.String
            && string.Equals(status.GetString(), "pending_approval", StringComparison.Ordinal)
            && value.TryGetProperty("actionId", out JsonElement id)
            && id.TryGetGuid(out Guid actionId)
                ? actionId
                : null;
}

public abstract class ChatEntry;

public sealed class UserEntry(string text) : ChatEntry
{
    public string Text { get; } = text;
}

/// <summary>The answer of the agent to one message.</summary>
public sealed class AgentEntry : ChatEntry
{
    private readonly List<ToolUse> _tools = [];
    private readonly List<ToolOutput> _outputs = [];
    private readonly List<ProposedAction> _actions = [];
    private readonly StringBuilder _text = new();
    private bool _toolsSinceText;

    public IReadOnlyList<ToolUse> Tools => _tools;

    public IReadOnlyList<ToolOutput> Outputs => _outputs;

    public IReadOnlyList<ProposedAction> Actions => _actions;

    public string Text => _text.ToString();

    public string? Error { get; private set; }

    public bool IsComplete { get; private set; }

    internal void AddTool(ToolUse tool)
    {
        _tools.Add(tool);
        _toolsSinceText = true;
    }

    internal void CompleteTool(string callId, string name, JsonElement? result, string? error)
    {
        ToolUse? tool = _tools.Find(t => string.Equals(t.CallId, callId, StringComparison.Ordinal));
        tool?.State = error is null ? ToolUseState.Succeeded : ToolUseState.Failed;

        if (error is null && result is { ValueKind: not JsonValueKind.Undefined and not JsonValueKind.Null } value)
        {
            _outputs.Add(new ToolOutput(callId, tool?.Name ?? name, value, tool?.Arguments));
        }
    }

    internal void MarkAwaitingApproval(string callId)
    {
        ToolUse? tool = _tools.Find(t => string.Equals(t.CallId, callId, StringComparison.Ordinal));
        tool?.State = ToolUseState.AwaitingApproval;
    }

    internal void Propose(ProposedAction action) => _actions.Add(action);

    internal void AppendText(string text)
    {
        // A chunk of spaces or line breaks is part of the text once the model streams token by token.
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // The text of a model call that follows tool calls starts a new paragraph.
        if (_toolsSinceText && _text.Length > 0)
        {
            _text.Append("\n\n");
        }

        _toolsSinceText = false;
        _text.Append(text);
    }

    internal void Fail(string message) => Error = message;

    internal void Complete() => IsComplete = true;
}

public enum ToolUseState
{
    Running,
    Succeeded,
    Failed,
    AwaitingApproval
}

public sealed class ToolUse(string callId, string name, bool isReadOnly, JsonElement? arguments)
{
    public string CallId { get; } = callId;

    public string Name { get; } = name;

    public bool IsReadOnly { get; } = isReadOnly;

    public JsonElement? Arguments { get; } = arguments;

    public ToolUseState State { get; internal set; }
}

/// <summary>The JSON result of a read tool, shown by the component of that tool (<see cref="ToolResults"/>).</summary>
public sealed record ToolOutput(string CallId, string ToolName, JsonElement Result, JsonElement? Arguments);

/// <summary>A write tool call waiting for the user's decision, then its outcome.</summary>
public sealed class ProposedAction(Guid actionId, string toolName, JsonElement arguments, string requiredScope)
{
    public Guid ActionId { get; } = actionId;

    public string ToolName { get; } = toolName;

    public JsonElement Arguments { get; } = arguments;

    public string RequiredScope { get; } = requiredScope;

    /// <summary>The batch of the turn that proposed it (an action proposed alone is a batch of one).</summary>
    public Guid BatchId { get; init; }

    public string Status { get; private set; } = ApiValues.Pending;

    public JsonElement? Result { get; private set; }

    public string? Error { get; private set; }

    public bool IsPending => string.Equals(Status, ApiValues.Pending, StringComparison.Ordinal);

    public static ProposedAction From(PendingActionDto action)
    {
        var proposed = new ProposedAction(action.Id, action.ToolName, action.Arguments, action.RequiredScope) { BatchId = action.BatchId };
        proposed.Update(action);

        return proposed;
    }

    public void Update(PendingActionDto action)
    {
        Status = action.Status;
        Result = action.Result;
        Error = action.Error;
    }
}
