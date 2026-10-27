namespace Pricely.Common.Application.AI.Agents;

/// <summary>
/// The agent run a tool is called for: its usage session (the conversation) and the agent. Set by the module that runs
/// the agents (Agentic) around each tool call, read by what the tool uses (web search, journal) to tie its cost and its
/// state to the conversation. Like <c>Activity.Current</c>, it flows with the asynchronous call, never across requests.
/// </summary>
/// <param name="SessionId">Usage session of the run (the conversation id), as in <see cref="LlmCalledIntegrationEvent.SessionId"/>.</param>
/// <param name="AgentName">Name of the agent.</param>
/// <param name="UserId">The user the agent runs for, if known.</param>
public sealed record AgentToolCall(string SessionId, string AgentName, string? UserId)
{
    private static readonly AsyncLocal<AgentToolCall?> Ambient = new();

    /// <summary>The tool call in progress on this asynchronous flow; null outside an agent (an MCP client, an endpoint).</summary>
    public static AgentToolCall? Current => Ambient.Value;

    /// <summary>Makes <paramref name="call"/> current until the returned scope is disposed.</summary>
    public static IDisposable Begin(AgentToolCall call)
    {
        AgentToolCall? previous = Ambient.Value;
        Ambient.Value = call;

        return new Scope(previous);
    }

    private sealed class Scope(AgentToolCall? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }
}
