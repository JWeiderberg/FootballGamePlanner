namespace FootballGamePlanner.Agents.GitHub;

/// <summary>
/// Reads from the real repository but only logs writes, so a run can be tried end to end
/// without creating issues, labels or comments. Created issues get fake numbers from 9000 up.
/// </summary>
internal sealed class DryRunGitHubClient(IGitHubClient inner, TextWriter log) : IGitHubClient
{
    private int _nextNumber = 9000;

    public Task<IReadOnlyList<GitHubIssue>> ListOpenIssuesAsync(string label, CancellationToken cancellationToken) =>
        inner.ListOpenIssuesAsync(label, cancellationToken);

    public Task<GitHubIssue> CreateIssueAsync(
        string title,
        string body,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken
    )
    {
        var issue = new GitHubIssue(_nextNumber++, title, body, "dry-run", labels);
        log.WriteLine($"[dry run] would create issue #{issue.Number} \"{title}\" [{string.Join(", ", labels)}]");
        log.WriteLine(body);
        return Task.FromResult(issue);
    }

    public Task AddLabelsAsync(int issueNumber, IReadOnlyList<string> labels, CancellationToken cancellationToken)
    {
        log.WriteLine($"[dry run] would add labels [{string.Join(", ", labels)}] to #{issueNumber}");
        return Task.CompletedTask;
    }

    public Task RemoveLabelAsync(int issueNumber, string label, CancellationToken cancellationToken)
    {
        log.WriteLine($"[dry run] would remove label {label} from #{issueNumber}");
        return Task.CompletedTask;
    }

    public Task AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken)
    {
        log.WriteLine($"[dry run] would comment on #{issueNumber}:");
        log.WriteLine(body);
        return Task.CompletedTask;
    }

    public Task EnsureLabelAsync(LabelDefinition label, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(label);
        log.WriteLine($"[dry run] would make sure label {label.Name} exists");
        return Task.CompletedTask;
    }
}
