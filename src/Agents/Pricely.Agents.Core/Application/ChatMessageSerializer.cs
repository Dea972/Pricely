using System.Text.Json;
using Microsoft.Extensions.AI;
using Pricely.Agents.Core.Domain;

namespace Pricely.Agents.Core.Application;

/// <summary>
/// Stores the messages exchanged with the model as they are (texts, tool calls, tool results) to replay them.
/// </summary>
internal static class ChatMessageSerializer
{
    private static readonly JsonSerializerOptions Options = new(AIJsonUtilities.DefaultOptions) { WriteIndented = false };

    public static string Serialize(ChatMessage message) => JsonSerializer.Serialize(message, Options);

    public static ChatMessage Deserialize(string content) =>
        JsonSerializer.Deserialize<ChatMessage>(content, Options) ?? throw new JsonException("Empty chat message.");

    public static string SerializeValue(object? value) => JsonSerializer.Serialize(value, Options);

    public static JsonElement ToElement(object? value) =>
        value is JsonElement element ? element.Clone() : JsonSerializer.SerializeToElement(value, Options);

    public static AgentMessageRole ToRole(ChatRole role)
    {
        if (role == ChatRole.User)
        {
            return AgentMessageRole.User;
        }

        if (role == ChatRole.Assistant)
        {
            return AgentMessageRole.Assistant;
        }

        return role == ChatRole.Tool ? AgentMessageRole.Tool : AgentMessageRole.System;
    }
}
