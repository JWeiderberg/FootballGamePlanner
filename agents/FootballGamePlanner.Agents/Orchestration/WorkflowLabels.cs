using FootballGamePlanner.Agents.GitHub;

namespace FootballGamePlanner.Agents.Orchestration;

/// <summary>
/// The labels that carry the workflow state. The orchestrator keeps no database: an issue's
/// labels are the whole truth about where its work stands.
/// </summary>
internal static class WorkflowLabels
{
    /// <summary>Put by the owner on an issue that asks the requirements agent for stories.</summary>
    public const string RunRequirements = "run:requirements";

    /// <summary>A user story created by the requirements agent.</summary>
    public const string Story = "story";

    /// <summary>A story waiting for the owner at checkpoint 1.</summary>
    public const string NeedsApproval = "needs-approval";

    /// <summary>Put by the owner on a story that passed checkpoint 1.</summary>
    public const string Approved = "approved";

    /// <summary>A request the agents have finished; the owner closes it when satisfied.</summary>
    public const string AgentDone = "agent-done";

    /// <summary>The agents stopped and need the owner, for example after hitting a limit.</summary>
    public const string NeedsHelp = "needs-help";

    public static IReadOnlyList<LabelDefinition> All { get; } =
    [
        new(RunRequirements, "5319e7", "Ask the requirements agent to write user stories"),
        new(Story, "1d76db", "User story"),
        new(NeedsApproval, "fbca04", "Waiting for the owner's approval (checkpoint 1)"),
        new(Approved, "0e8a16", "Approved by the owner"),
        new(AgentDone, "c2e0c6", "The agents have finished this request"),
        new(NeedsHelp, "d93f0b", "The agents stopped and need the owner"),
    ];
}
