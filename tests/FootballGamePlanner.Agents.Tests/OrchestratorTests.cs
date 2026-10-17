using FootballGamePlanner.Agents.Configuration;
using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Orchestration;

namespace FootballGamePlanner.Agents.Tests;

public sealed class OrchestratorTests : IDisposable
{
    private const string Owner = "JWeiderberg";

    private readonly TempRepository _repo = new();
    private readonly FakeGitHubClient _github = new();
    private readonly MemoryRunLog _runLog = new();

    public void Dispose() => _repo.Dispose();

    private Orchestrator Create(FakeModelClient model, long tokenBudget = 1_000_000) =>
        new(
            new AgentSettings
            {
                RepositoryOwner = Owner,
                RepositoryName = "FootballGamePlanner",
                TrustedLogin = Owner,
                RepositoryRoot = _repo.Root,
                TokenBudgetPerRun = tokenBudget,
            },
            _github,
            model,
            _runLog,
            TextWriter.Null,
            TimeProvider.System
        );

    private static FakeModelClient ModelThatCreatesOneStory() =>
        new(
            FakeModelClient.ToolCall(
                "create_story",
                new
                {
                    title = "Leader signs in and creates a team",
                    user_story = "As a leader, I want to create a team, so that I can plan its matches.",
                    acceptance_criteria = new[]
                    {
                        "Given I am signed in, when I create a team, then it appears in my team list.",
                    },
                    gdpr_requirements = new[] { "GDPR-03" },
                }
            ),
            FakeModelClient.Text("Created one story.")
        );

    [Fact]
    public async Task Makes_sure_the_workflow_labels_exist()
    {
        await Create(new FakeModelClient()).RunOnceAsync(CancellationToken.None);

        Assert.Equal(WorkflowLabels.All.Count, _github.RepositoryLabels.Count);
    }

    [Fact]
    public async Task Ignores_a_request_opened_by_anyone_but_the_owner()
    {
        var request = _github.Add("Please write stories", "someone-else", WorkflowLabels.RunRequirements);
        var model = new FakeModelClient();

        var handled = await Create(model).RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, handled);
        Assert.Empty(model.Requests);
        Assert.Empty(_github.Comments);
        Assert.Equal(new[] { WorkflowLabels.RunRequirements }, _github.Get(request.Number).Labels);
    }

    [Fact]
    public async Task Runs_the_requirements_agent_for_the_owners_request_and_reports_back()
    {
        var request = _github.Add("Stories for team setup", Owner, WorkflowLabels.RunRequirements);
        var model = ModelThatCreatesOneStory();

        var handled = await Create(model).RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, handled);
        Assert.Contains("Stories for team setup", ((TextPart)model.Requests[0].Messages[0].Parts[0]).Text, StringComparison.Ordinal);
        Assert.Equal("You write stories.", model.Requests[0].SystemPrompt);

        var story = Assert.Single(_github.Issues, i => i.HasLabel(WorkflowLabels.Story));
        Assert.True(story.HasLabel(WorkflowLabels.NeedsApproval));

        var (issue, comment) = Assert.Single(_github.Comments);
        Assert.Equal(request.Number, issue);
        Assert.Contains($"#{story.Number}", comment, StringComparison.Ordinal);
        Assert.Contains("Created one story.", comment, StringComparison.Ordinal);
        Assert.True(_github.Get(request.Number).HasLabel(WorkflowLabels.AgentDone));

        var entry = Assert.Single(_runLog.Entries);
        Assert.Equal("requirements", entry.Agent);
        Assert.Equal(new[] { story.Number }, entry.CreatedIssues);
    }

    [Fact]
    public async Task Does_not_run_a_request_twice()
    {
        _github.Add("Already done", Owner, WorkflowLabels.RunRequirements, WorkflowLabels.AgentDone);
        _github.Add("Stuck", Owner, WorkflowLabels.RunRequirements, WorkflowLabels.NeedsHelp);
        var model = new FakeModelClient();

        var handled = await Create(model).RunOnceAsync(CancellationToken.None);

        Assert.Equal(0, handled);
        Assert.Empty(model.Requests);
    }

    [Fact]
    public async Task Asks_the_owner_for_help_when_the_agent_is_stopped_by_a_limit()
    {
        var request = _github.Add("Too big", Owner, WorkflowLabels.RunRequirements);
        var model = new FakeModelClient(
            FakeModelClient.ToolCall("list_stories", new { }, tokens: 5_000),
            FakeModelClient.Text("never reached")
        );

        await Create(model, tokenBudget: 1_000).RunOnceAsync(CancellationToken.None);

        var updated = _github.Get(request.Number);
        Assert.True(updated.HasLabel(WorkflowLabels.NeedsHelp));
        Assert.False(updated.HasLabel(WorkflowLabels.AgentDone));
        Assert.Contains("token budget", Assert.Single(_github.Comments).Body, StringComparison.Ordinal);
    }
}
