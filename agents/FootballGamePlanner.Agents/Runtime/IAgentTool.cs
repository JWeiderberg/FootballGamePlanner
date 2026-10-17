using System.Text.Json;
using FootballGamePlanner.Agents.Llm;

namespace FootballGamePlanner.Agents.Runtime;

/// <summary>The outcome of one tool call, sent back to the model as a tool result.</summary>
internal sealed record ToolOutcome(string Content, bool IsError)
{
    public static ToolOutcome Ok(string content) => new(content, false);

    public static ToolOutcome Error(string message) => new(message, true);
}

/// <summary>
/// A capability an agent may use. Each agent gets only the tools its job needs; a tool
/// validates its own input and never trusts the model to stay within limits.
/// </summary>
internal interface IAgentTool
{
    ToolDefinition Definition { get; }

    Task<ToolOutcome> ExecuteAsync(IReadOnlyDictionary<string, JsonElement> input, CancellationToken cancellationToken);
}

/// <summary>Helpers for building tool schemas and reading tool input.</summary>
internal static class ToolInput
{
    public static JsonElement StringProperty(string description) =>
        JsonSerializer.SerializeToElement(new { type = "string", description });

    public static JsonElement StringArrayProperty(string description) =>
        JsonSerializer.SerializeToElement(new { type = "array", items = new { type = "string" }, description });

    /// <summary>Reads a required, non-empty string, or returns null.</summary>
    public static string? GetString(IReadOnlyDictionary<string, JsonElement> input, string name)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() is { Length: > 0 } text ? text : null
            : null;
    }

    /// <summary>Reads an optional array of strings; missing or wrong types give an empty list.</summary>
    public static IReadOnlyList<string> GetStringArray(IReadOnlyDictionary<string, JsonElement> input, string name)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. value
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => s.Length > 0),
        ];
    }
}
