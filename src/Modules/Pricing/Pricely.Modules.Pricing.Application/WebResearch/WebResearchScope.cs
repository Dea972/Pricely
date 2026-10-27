using Pricely.Common.Application.AI.Agents;
using Pricely.Common.Application.Authorization;

namespace Pricely.Modules.Pricing.Application.WebResearch;

/// <summary>The research a web search or a page read belongs to: the conversation of the agent, else the user.</summary>
internal static class WebResearchScope
{
    public static string Of(ICurrentUser currentUser) =>
        AgentToolCall.Current is { } call
            ? $"session:{call.SessionId}"
            : $"user:{currentUser.UserId?.ToString() ?? "anonymous"}";
}
