using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Pricely.Common.Application.Authorization;
using Pricely.Common.Presentation.Mcp;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// A tool declared by a module (<c>[McpServerTool]</c> method, ADR-009) turned into an <see cref="AIFunction"/> for the
/// agents: service parameters (such as <c>ISender</c>) are resolved from the services of the call, not exposed to the model.
/// </summary>
public sealed class AgentTool
{
    private AgentTool(string name, bool isReadOnly, string requiredScope, AIFunction function)
    {
        Name = name;
        IsReadOnly = isReadOnly;
        RequiredScope = requiredScope;
        Function = function;
    }

    public string Name { get; }

    /// <summary>A tool that is not read-only only runs after the user's approval.</summary>
    public bool IsReadOnly { get; }

    /// <summary>Scope needed to approve a write tool (ADR-010): <c>[ToolScope]</c>, <c>pricely.write</c> by default.</summary>
    public string RequiredScope { get; }

    public AIFunction Function { get; }

    public static AgentTool FromMethod(MethodInfo method)
    {
        McpServerToolAttribute attribute = method.GetCustomAttribute<McpServerToolAttribute>()
            ?? throw new ArgumentException($"{method.Name} is not a tool.", nameof(method));

        string name = attribute.Name ?? method.Name;

        AIFunction function = AIFunctionFactory.Create(
            method,
            target: null,
            new AIFunctionFactoryOptions
            {
                Name = name,
                ConfigureParameterBinding = parameter => parameter.ParameterType.IsInterface
                    ? new AIFunctionFactoryOptions.ParameterBindingOptions
                    {
                        ExcludeFromSchema = true,
                        BindParameter = (p, arguments) =>
                            (arguments.Services ?? throw new InvalidOperationException("The tool call has no services."))
                            .GetRequiredService(p.ParameterType)
                    }
                    : default
            });

        string requiredScope = attribute.ReadOnly
            ? PricelyScopes.Read
            : method.GetCustomAttribute<ToolScopeAttribute>()?.Scope ?? PricelyScopes.Write;

        return new AgentTool(name, attribute.ReadOnly, requiredScope, function);
    }
}
