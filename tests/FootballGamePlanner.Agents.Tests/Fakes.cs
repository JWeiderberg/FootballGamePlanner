using System.Text.Json;
using FootballGamePlanner.Agents.GitHub;
using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Orchestration;
using FootballGamePlanner.Agents.Runtime;

namespace FootballGamePlanner.Agents.Tests;

/// <summary>A model that replays scripted responses and records every request it gets.</summary>
internal sealed class FakeModelClient(params ModelResponse[] script) : IModelClient
{
    private readonly Queue<ModelResponse> _script = new(script);

    public List<ModelRequest> Requests { get; } = [];

    public Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        // Copy the message list: the runner keeps appending to the same list after the call.
        Requests.Add(request with { Messages = [.. request.Messages] });
        if (_script.Count == 0)
        {
            throw new InvalidOperationException("The fake model has no more scripted responses.");
        }

        return Task.FromResult(_script.Dequeue());
    }

    public static ModelResponse Text(string text, long tokens = 100) =>
        new([new TextPart(text)], StopKind.EndTurn, new TokenUsage(tokens, 0, 0, 0));

    public static ModelResponse ToolCall(string name, object input, string id = "call-1", long tokens = 100) =>
        new(
            [new ToolCallPart(id, name, Input(input))],
            StopKind.ToolUse,
            new TokenUsage(tokens, 0, 0, 0)
        );

    public static IReadOnlyDictionary<string, JsonElement> Input(object input) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(input))!;
}

/// <summary>An in-memory repository with issues, labels and comments.</summary>
internal sealed class FakeGitHubClient : IGitHubClient
{
    private int _nextNumber = 100;

    public List<GitHubIssue> Issues { get; } = [];

    public List<(int Issue, string Body)> Comments { get; } = [];

    public HashSet<string> RepositoryLabels { get; } = new(StringComparer.OrdinalIgnoreCase);

    public GitHubIssue Add(string title, string author, params string[] labels)
    {
        var issue = new GitHubIssue(_nextNumber++, title, "Body of " + title, author, labels);
        Issues.Add(issue);
        return issue;
    }

    public GitHubIssue Get(int number) => Issues.Single(i => i.Number == number);

    public Task<IReadOnlyList<GitHubIssue>> ListOpenIssuesAsync(string label, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GitHubIssue>>([.. Issues.Where(i => i.HasLabel(label))]);

    public Task<GitHubIssue> CreateIssueAsync(
        string title,
        string body,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken
    )
    {
        var issue = new GitHubIssue(_nextNumber++, title, body, "agent", labels);
        Issues.Add(issue);
        return Task.FromResult(issue);
    }

    public Task AddLabelsAsync(int issueNumber, IReadOnlyList<string> labels, CancellationToken cancellationToken)
    {
        Replace(issueNumber, existing => [.. existing.Union(labels)]);
        return Task.CompletedTask;
    }

    public Task RemoveLabelAsync(int issueNumber, string label, CancellationToken cancellationToken)
    {
        Replace(issueNumber, existing => [.. existing.Where(l => l != label)]);
        return Task.CompletedTask;
    }

    public Task AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken)
    {
        Comments.Add((issueNumber, body));
        return Task.CompletedTask;
    }

    public Task EnsureLabelAsync(LabelDefinition label, CancellationToken cancellationToken)
    {
        RepositoryLabels.Add(label.Name);
        return Task.CompletedTask;
    }

    private void Replace(int number, Func<IReadOnlyList<string>, IReadOnlyList<string>> change)
    {
        var index = Issues.FindIndex(i => i.Number == number);
        Issues[index] = Issues[index] with { Labels = change(Issues[index].Labels) };
    }
}

internal sealed class MemoryRunLog : IRunLog
{
    public List<RunLogEntry> Entries { get; } = [];

    public Task AppendAsync(RunLogEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

/// <summary>A tool that echoes its input, or throws when asked to.</summary>
internal sealed class EchoTool(string name = "echo") : IAgentTool
{
    public ToolDefinition Definition { get; } =
        new(
            name,
            "Echoes the text back.",
            new Dictionary<string, JsonElement> { ["text"] = ToolInput.StringProperty("Text to echo") },
            ["text"]
        );

    public Task<ToolOutcome> ExecuteAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken
    )
    {
        var text = ToolInput.GetString(input, "text");
        return text == "boom"
            ? throw new InvalidOperationException("boom happened")
            : Task.FromResult(ToolOutcome.Ok("echo: " + text));
    }
}

/// <summary>A temporary repository folder with docs/ and agents/prompts/, deleted after the test.</summary>
internal sealed class TempRepository : IDisposable
{
    public TempRepository()
    {
        Root = Path.Combine(Path.GetTempPath(), "fgp-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Root, "docs", "adr"));
        Directory.CreateDirectory(Path.Combine(Root, "agents", "prompts"));
        File.WriteAllText(Path.Combine(Root, "docs", "brief.md"), "# Brief\nLeaders plan matches.");
        File.WriteAllText(
            Path.Combine(Root, "docs", "gdpr.md"),
            "| GDPR-01 | minimal player data |\n| GDPR-03 | access per team |"
        );
        File.WriteAllText(Path.Combine(Root, "docs", "adr", "README.md"), "# ADRs");
        File.WriteAllText(Path.Combine(Root, "agents", "prompts", "requirements.md"), "You write stories.");
        File.WriteAllText(Path.Combine(Root, "secret.md"), "outside docs");
    }

    public string Root { get; }

    public string Docs => Path.Combine(Root, "docs");

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: a leftover temp folder must not fail a test.
        }
    }
}
