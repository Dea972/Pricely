namespace Pricely.Common.Infrastructure.Ai.Anthropic;

/// <summary>
/// Anthropic API settings (section <c>Ai:Anthropic</c>). The API key comes from user-secrets or the environment
/// (<c>Ai__Anthropic__ApiKey</c>), never from the repository.
/// </summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Ai:Anthropic";

    public Uri BaseAddress { get; init; } = new("https://api.anthropic.com/");

    public string ApiKey { get; init; } = string.Empty;

    public string ApiVersion { get; init; } = "2023-06-01";

    public string DefaultModel { get; init; } = "claude-sonnet-5-5";

    public int MaxTokens { get; init; } = 4096;

    public int TimeoutInSeconds { get; init; } = 120;
}
