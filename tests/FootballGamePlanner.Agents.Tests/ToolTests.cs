using FootballGamePlanner.Agents.Orchestration;
using FootballGamePlanner.Agents.Tools;

namespace FootballGamePlanner.Agents.Tests;

public sealed class DocumentToolTests : IDisposable
{
    private readonly TempRepository _repo = new();

    public void Dispose() => _repo.Dispose();

    [Fact]
    public void Lists_markdown_documents_relative_to_docs()
    {
        var store = new DocumentStore(_repo.Docs);

        Assert.Equal(new[] { "adr/README.md", "brief.md", "gdpr.md" }, store.List());
    }

    [Fact]
    public void Reads_a_document_inside_docs()
    {
        var store = new DocumentStore(_repo.Docs);

        Assert.Contains("Leaders plan matches", store.Read("brief.md"), StringComparison.Ordinal);
        Assert.Equal("# ADRs", store.Read("adr/README.md"));
    }

    [Theory]
    [InlineData("../secret.md")]
    [InlineData("adr/../../secret.md")]
    [InlineData("/etc/passwd.md")]
    [InlineData("brief.txt")]
    [InlineData("missing.md")]
    public void Refuses_anything_outside_docs_or_not_markdown(string path)
    {
        var store = new DocumentStore(_repo.Docs);

        Assert.Null(store.Read(path));
    }

    [Fact]
    public async Task Read_tool_returns_an_error_for_a_refused_path()
    {
        var tool = new ReadDocumentTool(new DocumentStore(_repo.Docs));

        var outcome = await tool.ExecuteAsync(FakeModelClient.Input(new { path = "../secret.md" }), CancellationToken.None);

        Assert.True(outcome.IsError);
        Assert.DoesNotContain("outside docs", outcome.Content, StringComparison.Ordinal);
    }
}

public sealed class CreateStoryToolTests
{
    private static readonly HashSet<string> KnownIds = ["GDPR-01", "GDPR-03"];

    private static object ValidStory(string title = "Leader signs in and creates a team") =>
        new
        {
            title,
            user_story = "As a leader, I want to create a team, so that I can plan its matches.",
            acceptance_criteria = new[]
            {
                "Given I am signed in, when I create a team named P2015, then it appears in my team list.",
                "Given I am not signed in, when I open the app, then I am asked to sign in.",
            },
            gdpr_requirements = new[] { "GDPR-03" },
            notes = "Invites are a separate story.",
        };

    [Fact]
    public async Task Creates_a_story_waiting_for_approval()
    {
        var github = new FakeGitHubClient();
        var tool = new CreateStoryTool(github, KnownIds, requestIssueNumber: 7, maxStories: 5);

        var outcome = await tool.ExecuteAsync(FakeModelClient.Input(ValidStory()), CancellationToken.None);

        Assert.False(outcome.IsError, outcome.Content);
        var issue = Assert.Single(github.Issues);
        Assert.Equal("Leader signs in and creates a team", issue.Title);
        Assert.Equal(new[] { WorkflowLabels.Story, WorkflowLabels.NeedsApproval }, issue.Labels);
        Assert.Contains("- [ ] Given I am signed in", issue.Body, StringComparison.Ordinal);
        Assert.Contains("GDPR-03", issue.Body, StringComparison.Ordinal);
        Assert.Contains("for #7", issue.Body, StringComparison.Ordinal);
        Assert.Same(issue, Assert.Single(tool.Created));
    }

    [Fact]
    public async Task Rejects_a_story_that_breaks_the_format_and_creates_nothing()
    {
        var github = new FakeGitHubClient();
        var tool = new CreateStoryTool(github, KnownIds, requestIssueNumber: 7, maxStories: 5);
        var input = FakeModelClient.Input(
            new
            {
                title = "Teams",
                user_story = "Leaders need teams.",
                acceptance_criteria = new[] { "It works." },
                gdpr_requirements = new[] { "GDPR-99" },
            }
        );

        var outcome = await tool.ExecuteAsync(input, CancellationToken.None);

        Assert.True(outcome.IsError);
        Assert.Contains("user_story", outcome.Content, StringComparison.Ordinal);
        Assert.Contains("Given/when/then", outcome.Content, StringComparison.Ordinal);
        Assert.Contains("GDPR-99", outcome.Content, StringComparison.Ordinal);
        Assert.Empty(github.Issues);
    }

    [Fact]
    public async Task Rejects_a_title_that_is_too_long()
    {
        var github = new FakeGitHubClient();
        var tool = new CreateStoryTool(github, KnownIds, requestIssueNumber: 7, maxStories: 5);

        var outcome = await tool.ExecuteAsync(
            FakeModelClient.Input(ValidStory(new string('x', 101))),
            CancellationToken.None
        );

        Assert.True(outcome.IsError);
        Assert.Empty(github.Issues);
    }

    [Fact]
    public async Task Stops_creating_stories_at_the_limit()
    {
        var github = new FakeGitHubClient();
        var tool = new CreateStoryTool(github, KnownIds, requestIssueNumber: 7, maxStories: 1);

        await tool.ExecuteAsync(FakeModelClient.Input(ValidStory("First")), CancellationToken.None);
        var second = await tool.ExecuteAsync(FakeModelClient.Input(ValidStory("Second")), CancellationToken.None);

        Assert.True(second.IsError);
        Assert.Single(github.Issues);
    }

    [Fact]
    public void Finds_the_gdpr_ids_in_the_gdpr_document()
    {
        var ids = GdprIds.Parse("| GDPR-01 | a |\n| GDPR-12 | b |\nrequirements GDPR-01 to GDPR-12");

        Assert.Equal(new HashSet<string> { "GDPR-01", "GDPR-12" }, ids);
    }
}
