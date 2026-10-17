using System.Globalization;
using System.Text;
using FootballGamePlanner.Agents.Configuration;
using FootballGamePlanner.Agents.GitHub;
using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Runtime;
using FootballGamePlanner.Agents.Tools;

namespace FootballGamePlanner.Agents.Orchestration;

/// <summary>
/// Deterministic code that decides which agent runs next. It reads the workflow state from
/// issue labels, acts only on issues opened by the trusted owner, runs one agent at a time,
/// and reports back on the issue. It keeps no state of its own.
/// </summary>
internal sealed class Orchestrator(
    AgentSettings settings,
    IGitHubClient github,
    IModelClient model,
    IRunLog runLog,
    TextWriter log,
    TimeProvider time
)
{
    /// <summary>Runs every pending request once, then returns the number of requests handled.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var label in WorkflowLabels.All)
        {
            await github.EnsureLabelAsync(label, cancellationToken).ConfigureAwait(false);
        }

        var requests = await github
            .ListOpenIssuesAsync(WorkflowLabels.RunRequirements, cancellationToken)
            .ConfigureAwait(false);

        var handled = 0;
        foreach (var request in requests.OrderBy(r => r.Number))
        {
            if (!IsTrusted(request))
            {
                // Public repository: anyone can open an issue and add text. Never pass it on to an agent,
                // and do not reply, so the orchestrator cannot be used to talk to strangers.
                log.WriteLine($"Skipping #{request.Number}: opened by {request.AuthorLogin}, not {settings.TrustedLogin}.");
                continue;
            }

            if (request.HasLabel(WorkflowLabels.AgentDone) || request.HasLabel(WorkflowLabels.NeedsHelp))
            {
                continue;
            }

            await RunRequirementsAgentAsync(request, cancellationToken).ConfigureAwait(false);
            handled++;
        }

        log.WriteLine(handled == 0 ? "Nothing to do." : $"Handled {handled} request(s).");
        return handled;
    }

    internal bool IsTrusted(GitHubIssue issue) =>
        string.Equals(issue.AuthorLogin, settings.TrustedLogin, StringComparison.OrdinalIgnoreCase);

    private async Task RunRequirementsAgentAsync(GitHubIssue request, CancellationToken cancellationToken)
    {
        log.WriteLine($"Running the requirements agent for #{request.Number} \"{request.Title}\"...");
        var startedAt = time.GetUtcNow();

        var documents = new DocumentStore(settings.DocsDirectory);
        var gdprIds = GdprIds.Parse(documents.Read("gdpr.md") ?? string.Empty);
        var createStory = new CreateStoryTool(github, gdprIds, request.Number, settings.MaxStoriesPerRun);

        var prompt = await File.ReadAllTextAsync(
                Path.Combine(settings.PromptsDirectory, "requirements.md"),
                cancellationToken
            )
            .ConfigureAwait(false);

        var agent = new AgentSpec(
            "requirements",
            settings.RequirementsModel,
            prompt,
            [new ListDocumentsTool(documents), new ReadDocumentTool(documents), new ListStoriesTool(github), createStory]
        );

        var task = string.Create(
            CultureInfo.InvariantCulture,
            $"""
            The owner has opened request #{request.Number}, asking for user stories.

            Title: {request.Title}

            {request.Body}
            """
        );

        var result = await new AgentRunner(model)
            .RunAsync(agent, task, new AgentLimits(settings.MaxTurnsPerRun, settings.TokenBudgetPerRun), cancellationToken)
            .ConfigureAwait(false);

        var created = createStory.Created.Select(i => i.Number).ToList();
        await runLog
            .AppendAsync(RunLogEntry.From(startedAt, agent.Name, request.Number, result, created), cancellationToken)
            .ConfigureAwait(false);

        await github
            .AddCommentAsync(request.Number, BuildReport(result, createStory.Created), cancellationToken)
            .ConfigureAwait(false);

        var finalLabel = result.Outcome == RunOutcome.Completed ? WorkflowLabels.AgentDone : WorkflowLabels.NeedsHelp;
        await github.AddLabelsAsync(request.Number, [finalLabel], cancellationToken).ConfigureAwait(false);

        log.WriteLine($"#{request.Number}: {result.Outcome}, {created.Count} stories, {result.Usage.Total} tokens.");
    }

    internal static string BuildReport(AgentRunResult result, IReadOnlyList<GitHubIssue> created)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(created);

        var report = new StringBuilder();
        report.AppendLine(
            result.Outcome switch
            {
                RunOutcome.Completed => "**The requirements agent has finished.**",
                RunOutcome.BudgetExceeded => "**The requirements agent stopped: it used up its token budget.**",
                RunOutcome.TurnLimitReached => "**The requirements agent stopped: it reached its turn limit.**",
                _ => "**The requirements agent stopped unexpectedly.**",
            }
        );
        report.AppendLine();

        if (created.Count > 0)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"Stories created ({created.Count}), each waiting for your approval:");
            report.AppendLine();
            foreach (var issue in created)
            {
                report.AppendLine(CultureInfo.InvariantCulture, $"- #{issue.Number} {issue.Title}");
            }

            report.AppendLine();
        }

        if (result.FinalText.Length > 0)
        {
            report.AppendLine("### Summary from the agent").AppendLine().AppendLine(result.FinalText).AppendLine();
        }

        report.AppendLine(
            CultureInfo.InvariantCulture,
            $"<sub>{result.Turns} model calls · {result.ToolCalls.Count} tool calls · "
                + $"tokens: {result.Usage.Input} in, {result.Usage.Output} out, "
                + $"{result.Usage.CacheRead} cache read, {result.Usage.CacheWrite} cache write</sub>"
        );
        return report.ToString();
    }
}
