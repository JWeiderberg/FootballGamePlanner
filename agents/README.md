# Agents

The orchestrator and the AI agents that build Football Game Planner. The design is in [docs/agent-system.md](../docs/agent-system.md).

So far there is one agent, the **requirements agent**. It turns the product documents into user stories, created as GitHub issues that wait for the owner's approval.

## How it works

1. The owner opens an issue describing which stories are wanted, for example "Write the stories for team setup and inviting leaders", and adds the label `run:requirements`.
2. The owner runs the orchestrator (below). It picks up every open `run:requirements` issue **opened by the owner** and runs the requirements agent on it. Issues opened by anyone else are ignored.
3. The agent reads `docs/`, checks the existing stories, and creates new ones labelled `story` and `needs-approval`.
4. The orchestrator comments on the request with the stories created, the agent's summary and its token usage, and labels the request `agent-done`. If the agent hit a limit, the label is `needs-help` instead.
5. The owner reviews each story: adds `approved` (checkpoint 1), or comments with changes and edits the story.

To run a request again, remove `agent-done` or `needs-help` from it.

| Label | Meaning |
| --- | --- |
| `run:requirements` | The owner asks the requirements agent for stories |
| `story` | A user story |
| `needs-approval` | Waiting for the owner (checkpoint 1) |
| `approved` | Approved by the owner |
| `agent-done` | The agents have finished this request |
| `needs-help` | The agents stopped and need the owner |

The orchestrator creates these labels if they are missing.

## Running it locally

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a local clone of this repository.

### 1. Secrets

The orchestrator reads two secrets from environment variables. Never put them in a file in the repository.

- `ANTHROPIC_API_KEY`: your Anthropic API key. Set a low monthly spend limit for it in the Claude Console.
- `GITHUB_TOKEN`: a **fine-grained personal access token** limited to this repository:
  - GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token
  - Repository access: **Only select repositories** → `FootballGamePlanner`
  - Repository permissions: **Issues: Read and write** (Metadata: Read-only is added automatically). Nothing else.
  - Give it an expiry date.

macOS or Linux:

```bash
export ANTHROPIC_API_KEY=...
export GITHUB_TOKEN=...
```

Windows PowerShell:

```powershell
$env:ANTHROPIC_API_KEY = "..."
$env:GITHUB_TOKEN = "..."
```

### 2. Try a dry run first

A dry run reads issues from GitHub and calls the model for real, but only prints what it would write: no issues, labels or comments are created.

```bash
dotnet run --project agents/FootballGamePlanner.Agents -- run --dry-run
```

### 3. Run for real

```bash
dotnet run --project agents/FootballGamePlanner.Agents -- run
```

Every run is also logged locally, one JSON line per agent run, in `.agent-runs/runs.jsonl` (ignored by git).

### Optional settings

| Variable | Default | Meaning |
| --- | --- | --- |
| `FGP_REPOSITORY` | `JWeiderberg/FootballGamePlanner` | Repository to work on |
| `FGP_TRUSTED_LOGIN` | the repository owner | The only account whose requests are acted on |

Limits per agent run: 40 model calls, 600,000 tokens and 30 stories. A run that hits a limit stops and asks for help.

## Layout

```
FootballGamePlanner.Agents/
  Program.cs              command line entry point
  Orchestration/          the orchestrator, workflow labels, run log
  Runtime/                the tool-use loop shared by all agents
  Llm/                    model abstraction and the Anthropic SDK adapter
  GitHub/                 GitHub REST client and the dry-run wrapper
  Tools/                  the tools agents may use
prompts/
  requirements.md         instructions for the requirements agent
```

Tests are in `tests/FootballGamePlanner.Agents.Tests`. They use a scripted fake model and an in-memory GitHub, so they never call the real API or create real issues.
