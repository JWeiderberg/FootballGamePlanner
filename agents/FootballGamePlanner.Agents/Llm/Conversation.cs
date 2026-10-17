using System.Text.Json;

namespace FootballGamePlanner.Agents.Llm;

/// <summary>Who wrote a message in a conversation with the model.</summary>
internal enum ConversationRole
{
    User,
    Assistant,
}

/// <summary>One piece of a message: text, a tool call from the model, or a tool result sent back.</summary>
internal abstract record ContentPart;

internal sealed record TextPart(string Text) : ContentPart;

internal sealed record ToolCallPart(string Id, string Name, IReadOnlyDictionary<string, JsonElement> Input)
    : ContentPart;

internal sealed record ToolResultPart(string ToolCallId, string Content, bool IsError) : ContentPart;

internal sealed record ConversationMessage(ConversationRole Role, IReadOnlyList<ContentPart> Parts)
{
    public static ConversationMessage UserText(string text) =>
        new(ConversationRole.User, [new TextPart(text)]);
}

/// <summary>A tool the model may call. The input schema is a JSON Schema object.</summary>
internal sealed record ToolDefinition(
    string Name,
    string Description,
    IReadOnlyDictionary<string, JsonElement> Properties,
    IReadOnlyList<string> Required
);

/// <summary>Why the model stopped producing output.</summary>
internal enum StopKind
{
    EndTurn,
    ToolUse,
    MaxTokens,
    Other,
}

/// <summary>Token counts for one or more model calls.</summary>
internal readonly record struct TokenUsage(long Input, long Output, long CacheRead, long CacheWrite)
{
    /// <summary>All tokens that count toward a budget.</summary>
    public long Total => Input + Output + CacheRead + CacheWrite;

    public static TokenUsage operator +(TokenUsage left, TokenUsage right) =>
        new(
            left.Input + right.Input,
            left.Output + right.Output,
            left.CacheRead + right.CacheRead,
            left.CacheWrite + right.CacheWrite
        );

    public static TokenUsage Add(TokenUsage left, TokenUsage right) => left + right;
}

internal sealed record ModelRequest(
    string Model,
    string SystemPrompt,
    IReadOnlyList<ConversationMessage> Messages,
    IReadOnlyList<ToolDefinition> Tools,
    int MaxTokens
);

internal sealed record ModelResponse(IReadOnlyList<ContentPart> Parts, StopKind Stop, TokenUsage Usage);

/// <summary>
/// The only way the agents talk to a language model. The real implementation uses the
/// Anthropic SDK; tests use a scripted fake, so no test ever calls the real API.
/// </summary>
internal interface IModelClient
{
    Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken);
}
