namespace FootballGamePlanner.Agents.GitHub;

/// <summary>An issue as the orchestrator sees it. Pull requests are filtered out before this.</summary>
internal sealed record GitHubIssue(
    int Number,
    string Title,
    string Body,
    string AuthorLogin,
    IReadOnlyList<string> Labels
)
{
    public bool HasLabel(string label) => Labels.Contains(label, StringComparer.OrdinalIgnoreCase);
}

/// <summary>A label the agent system depends on.</summary>
internal sealed record LabelDefinition(string Name, string Color, string Description);

/// <summary>
/// The GitHub operations the agent system needs, and nothing more. The real
/// implementation calls the REST API; tests use an in-memory fake.
/// </summary>
internal interface IGitHubClient
{
    Task<IReadOnlyList<GitHubIssue>> ListOpenIssuesAsync(string label, CancellationToken cancellationToken);

    Task<GitHubIssue> CreateIssueAsync(
        string title,
        string body,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken
    );

    Task AddLabelsAsync(int issueNumber, IReadOnlyList<string> labels, CancellationToken cancellationToken);

    Task RemoveLabelAsync(int issueNumber, string label, CancellationToken cancellationToken);

    Task AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken);

    /// <summary>Creates the label if it is missing; leaves an existing label untouched.</summary>
    Task EnsureLabelAsync(LabelDefinition label, CancellationToken cancellationToken);
}
