using Microsoft.Extensions.AI;

namespace Pricely.Common.Application.AI.Agents;

/// <summary>
/// A tool the agents may call. A tool that is not read-only is never run by the model: the Agentic module turns the
/// call into an action that a person approves (with <see cref="ApprovalScope"/>, or the module's default scope).
/// Service parameters of <see cref="Function"/> are resolved from the services of the call.
/// </summary>
public sealed record AgentTool(string Name, bool IsReadOnly, string? ApprovalScope, AIFunction Function);
