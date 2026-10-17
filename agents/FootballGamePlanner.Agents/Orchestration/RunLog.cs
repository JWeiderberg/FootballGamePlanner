using System.Text.Json;
using FootballGamePlanner.Agents.Runtime;

namespace FootballGamePlanner.Agents.Orchestration;

/// <summary>One line in the local run log: which agent ran on what, and what it cost.</summary>
internal sealed record RunLogEntry(
    DateTimeOffset StartedAt,
    string Agent,
    int RequestIssue,
    string Outcome,
    int Turns,
    long InputTokens,
    long OutputTokens,
    long CacheReadTokens,
    long CacheWriteTokens,
    int ToolCalls,
    int FailedToolCalls,
    IReadOnlyList<int> CreatedIssues
)
{
    public static RunLogEntry From(
        DateTimeOffset startedAt,
        string agent,
        int requestIssue,
        AgentRunResult result,
        IReadOnlyList<int> createdIssues
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        return new(
            startedAt,
            agent,
            requestIssue,
            result.Outcome.ToString(),
            result.Turns,
            result.Usage.Input,
            result.Usage.Output,
            result.Usage.CacheRead,
            result.Usage.CacheWrite,
            result.ToolCalls.Count,
            result.ToolCalls.Count(c => c.IsError),
            createdIssues
        );
    }
}

internal interface IRunLog
{
    Task AppendAsync(RunLogEntry entry, CancellationToken cancellationToken);
}

/// <summary>Appends JSON lines to .agent-runs/runs.jsonl in the local checkout (ignored by git).</summary>
internal sealed class FileRunLog(string directory) : IRunLog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AppendAsync(RunLogEntry entry, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);
        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(Path.Combine(directory, "runs.jsonl"), line, cancellationToken)
            .ConfigureAwait(false);
    }
}
