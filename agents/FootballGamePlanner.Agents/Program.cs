using Anthropic;
using FootballGamePlanner.Agents.Configuration;
using FootballGamePlanner.Agents.GitHub;
using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Orchestration;

namespace FootballGamePlanner.Agents;

/// <summary>
/// Command line entry point. Usage:
///   dotnet run --project agents/FootballGamePlanner.Agents -- run [--dry-run] [--repo-root &lt;path&gt;]
/// Secrets come from the environment: ANTHROPIC_API_KEY and GITHUB_TOKEN.
/// </summary>
internal static class Program
{
    private const string DefaultRepository = "JWeiderberg/FootballGamePlanner";

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "run")
        {
            await Console.Error.WriteLineAsync(
                "Usage: FootballGamePlanner.Agents run [--dry-run] [--repo-root <path>]"
            ).ConfigureAwait(false);
            return 2;
        }

        var dryRun = args.Contains("--dry-run");
        var repoRoot = FindRepositoryRoot(GetOption(args, "--repo-root") ?? Directory.GetCurrentDirectory());
        if (repoRoot is null)
        {
            await Console.Error.WriteLineAsync(
                "Could not find the repository root (a folder containing docs/brief.md). Use --repo-root."
            ).ConfigureAwait(false);
            return 2;
        }

        var repository = Environment.GetEnvironmentVariable("FGP_REPOSITORY") ?? DefaultRepository;
        var parts = repository.Split('/', 2);
        if (parts.Length != 2)
        {
            await Console.Error.WriteLineAsync("FGP_REPOSITORY must look like owner/name.").ConfigureAwait(false);
            return 2;
        }

        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        var hasApiKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
        if (string.IsNullOrEmpty(githubToken) || !hasApiKey)
        {
            await Console.Error.WriteLineAsync(
                "Set ANTHROPIC_API_KEY and GITHUB_TOKEN in the environment (see agents/README.md)."
            ).ConfigureAwait(false);
            return 2;
        }

        var settings = new AgentSettings
        {
            RepositoryOwner = parts[0],
            RepositoryName = parts[1],
            TrustedLogin = Environment.GetEnvironmentVariable("FGP_TRUSTED_LOGIN") ?? parts[0],
            RepositoryRoot = repoRoot,
            DryRun = dryRun,
        };

        using var http = new HttpClient();
        IGitHubClient github = new GitHubRestClient(http, settings.RepositoryOwner, settings.RepositoryName, githubToken);
        if (dryRun)
        {
            github = new DryRunGitHubClient(github, Console.Out);
        }

        // Reads ANTHROPIC_API_KEY from the environment.
        using var anthropic = new AnthropicClient();
        var orchestrator = new Orchestrator(
            settings,
            github,
            new AnthropicModelClient(anthropic),
            new FileRunLog(settings.RunLogDirectory),
            Console.Out,
            TimeProvider.System
        );

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        Console.WriteLine(
            $"Orchestrator for {settings.RepositoryOwner}/{settings.RepositoryName}, trusting {settings.TrustedLogin}"
                + (dryRun ? " (dry run: nothing is written to GitHub)" : string.Empty)
        );
        await orchestrator.RunOnceAsync(cancellation.Token).ConfigureAwait(false);
        return 0;
    }

    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string? FindRepositoryRoot(string start)
    {
        for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "brief.md")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
