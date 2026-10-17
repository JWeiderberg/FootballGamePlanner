using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FootballGamePlanner.Agents.GitHub;
using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Orchestration;
using FootballGamePlanner.Agents.Runtime;

namespace FootballGamePlanner.Agents.Tools;

internal sealed class ListStoriesTool(IGitHubClient github) : IAgentTool
{
    public ToolDefinition Definition { get; } =
        new(
            "list_stories",
            "Lists the open user stories that already exist, with number, title and status labels. "
                + "Check this before creating stories so you never create a duplicate.",
            new Dictionary<string, JsonElement>(),
            []
        );

    public async Task<ToolOutcome> ExecuteAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken
    )
    {
        var stories = await github.ListOpenIssuesAsync(WorkflowLabels.Story, cancellationToken).ConfigureAwait(false);
        if (stories.Count == 0)
        {
            return ToolOutcome.Ok("There are no open stories yet.");
        }

        var lines = stories
            .OrderBy(s => s.Number)
            .Select(s => string.Create(CultureInfo.InvariantCulture, $"#{s.Number} {s.Title} [{string.Join(", ", s.Labels)}]"));
        return ToolOutcome.Ok(string.Join("\n", lines));
    }
}

/// <summary>
/// Creates one user story as a GitHub issue, waiting for the owner's approval. Every field is
/// validated here, so a story that breaks the format never reaches GitHub; the model gets the
/// reasons back and can correct itself.
/// </summary>
internal sealed partial class CreateStoryTool(
    IGitHubClient github,
    IReadOnlySet<string> knownGdprIds,
    int requestIssueNumber,
    int maxStories
) : IAgentTool
{
    private const int MaxTitleLength = 100;

    private readonly List<GitHubIssue> _created = [];

    public IReadOnlyList<GitHubIssue> Created => _created;

    public ToolDefinition Definition { get; } =
        new(
            "create_story",
            "Creates one user story as a GitHub issue, labelled as waiting for the owner's approval.",
            new Dictionary<string, JsonElement>
            {
                ["title"] = ToolInput.StringProperty("Short imperative title, at most 100 characters"),
                ["user_story"] = ToolInput.StringProperty(
                    "One sentence: \"As a <role>, I want <capability>, so that <benefit>.\""
                ),
                ["acceptance_criteria"] = ToolInput.StringArrayProperty(
                    "Testable criteria, each one sentence in the form \"Given ..., when ..., then ...\""
                ),
                ["gdpr_requirements"] = ToolInput.StringArrayProperty(
                    "Ids of the GDPR requirements this story must satisfy, for example GDPR-01; empty if none apply"
                ),
                ["notes"] = ToolInput.StringProperty(
                    "Optional: scope limits, assumptions or open questions for the owner"
                ),
            },
            ["title", "user_story", "acceptance_criteria", "gdpr_requirements"]
        );

    public async Task<ToolOutcome> ExecuteAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken
    )
    {
        if (_created.Count >= maxStories)
        {
            return ToolOutcome.Error(
                $"You have created {maxStories} stories, the limit for one run. Stop and summarise what is left."
            );
        }

        var title = ToolInput.GetString(input, "title")?.Trim();
        var userStory = ToolInput.GetString(input, "user_story")?.Trim();
        var criteria = ToolInput.GetStringArray(input, "acceptance_criteria");
        var gdprIds = ToolInput.GetStringArray(input, "gdpr_requirements");
        var notes = ToolInput.GetString(input, "notes")?.Trim();

        var problems = Validate(title, userStory, criteria, gdprIds);
        if (problems.Count > 0)
        {
            return ToolOutcome.Error("The story was not created:\n- " + string.Join("\n- ", problems));
        }

        var body = BuildBody(userStory!, criteria, gdprIds, notes, requestIssueNumber);
        var issue = await github
            .CreateIssueAsync(title!, body, [WorkflowLabels.Story, WorkflowLabels.NeedsApproval], cancellationToken)
            .ConfigureAwait(false);
        _created.Add(issue);

        return ToolOutcome.Ok(string.Create(CultureInfo.InvariantCulture, $"Created story #{issue.Number}: {issue.Title}"));
    }

    internal List<string> Validate(
        string? title,
        string? userStory,
        IReadOnlyList<string> criteria,
        IReadOnlyList<string> gdprIds
    )
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            problems.Add("title is missing.");
        }
        else if (title.Length > MaxTitleLength)
        {
            problems.Add($"title is longer than {MaxTitleLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(userStory) || !UserStoryPattern().IsMatch(userStory))
        {
            problems.Add("user_story must read \"As a <role>, I want <capability>, so that <benefit>.\"");
        }

        if (criteria.Count == 0)
        {
            problems.Add("acceptance_criteria needs at least one criterion.");
        }

        foreach (var criterion in criteria.Where(c => !CriterionPattern().IsMatch(c)))
        {
            problems.Add($"acceptance criterion is not in Given/when/then form: \"{criterion}\"");
        }

        foreach (var id in gdprIds.Where(id => !knownGdprIds.Contains(id)))
        {
            problems.Add($"\"{id}\" is not a GDPR requirement id in docs/gdpr.md.");
        }

        return problems;
    }

    private static string BuildBody(
        string userStory,
        IReadOnlyList<string> criteria,
        IReadOnlyList<string> gdprIds,
        string? notes,
        int requestIssueNumber
    )
    {
        var body = new StringBuilder();
        body.AppendLine("## User story").AppendLine().AppendLine(userStory).AppendLine();
        body.AppendLine("## Acceptance criteria").AppendLine();
        foreach (var criterion in criteria)
        {
            body.Append("- [ ] ").AppendLine(criterion);
        }

        body.AppendLine().AppendLine("## GDPR requirements").AppendLine();
        body.AppendLine(gdprIds.Count == 0 ? "None." : string.Join(", ", gdprIds.Order(StringComparer.Ordinal)));

        if (!string.IsNullOrWhiteSpace(notes))
        {
            body.AppendLine().AppendLine("## Notes").AppendLine().AppendLine(notes);
        }

        body.AppendLine()
            .AppendLine("---")
            .AppendLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Written by the requirements agent for #{requestIssueNumber}. "
                )
                    + $"Add the `{WorkflowLabels.Approved}` label to approve it, or comment with changes."
            );
        return body.ToString();
    }

    [GeneratedRegex(@"^As an? .+, I want .+, so that .+\.$", RegexOptions.Singleline)]
    private static partial Regex UserStoryPattern();

    [GeneratedRegex(@"^Given .+, when .+, then .+$", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CriterionPattern();
}

/// <summary>Finds the GDPR requirement ids (GDPR-01, GDPR-02, ...) defined in docs/gdpr.md.</summary>
internal static partial class GdprIds
{
    public static IReadOnlySet<string> Parse(string gdprDocument)
    {
        ArgumentNullException.ThrowIfNull(gdprDocument);
        return IdPattern().Matches(gdprDocument).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
    }

    [GeneratedRegex(@"GDPR-\d{2}")]
    private static partial Regex IdPattern();
}
