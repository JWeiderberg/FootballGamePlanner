namespace FootballGamePlanner.Agents.Configuration;

/// <summary>Settings for one orchestrator run. Secrets are not part of this; they come from the environment.</summary>
internal sealed record AgentSettings
{
    public required string RepositoryOwner { get; init; }

    public required string RepositoryName { get; init; }

    /// <summary>
    /// The only GitHub account whose issues and labels the orchestrator acts on. The repository
    /// is public, so content from anyone else is ignored and never reaches an agent.
    /// </summary>
    public required string TrustedLogin { get; init; }

    /// <summary>Local checkout of the repository; agents read docs/ and prompts from here.</summary>
    public required string RepositoryRoot { get; init; }

    public string RequirementsModel { get; init; } = "claude-opus-5-5";

    /// <summary>Maximum model calls in one agent run.</summary>
    public int MaxTurnsPerRun { get; init; } = 40;

    /// <summary>Maximum tokens (input, output and cache) in one agent run.</summary>
    public long TokenBudgetPerRun { get; init; } = 600_000;

    /// <summary>Maximum stories the requirements agent may create in one run.</summary>
    public int MaxStoriesPerRun { get; init; } = 30;

    /// <summary>Read from GitHub as usual, but log every write instead of performing it.</summary>
    public bool DryRun { get; init; }

    public string PromptsDirectory => Path.Combine(RepositoryRoot, "agents", "prompts");

    public string DocsDirectory => Path.Combine(RepositoryRoot, "docs");

    public string RunLogDirectory => Path.Combine(RepositoryRoot, ".agent-runs");
}
