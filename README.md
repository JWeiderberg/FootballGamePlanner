# Football Game Planner

Match planning for football coaches: squad, line-up, planned substitutions and fair playing time, from a phone.

The app is built story by story by an AI agent system, with checkpoints where a human approves. See [docs/agent-system.md](docs/agent-system.md).

The app's user interface is in Swedish for the first version. Everything in this repository (code, documentation, issues and pull requests) is in English.

## Documents

| Document | Contents |
| --- | --- |
| [docs/brief.md](docs/brief.md) | Product brief: vision, MVP scope, substitution and playing-time rules |
| [docs/gdpr.md](docs/gdpr.md) | GDPR analysis and requirements GDPR-01 to GDPR-12 |
| [docs/agent-system.md](docs/agent-system.md) | The agent system: agents, checkpoints, permissions, test strategy |
| [docs/adr/](docs/adr/) | Architecture decision records |

The documents in `docs/` are what the agents read and work from.

## Structure

```
src/
  FootballGamePlanner.Web/             Blazor web app (mobile-first, becomes a PWA)
  FootballGamePlanner.Domain/          domain model and the line-up suggestion algorithm
  FootballGamePlanner.Infrastructure/  database and sign-in
agents/
  FootballGamePlanner.Agents/          the orchestrator and the AI agents (see agents/README.md)
  prompts/                             one instruction file per agent
tests/
  FootballGamePlanner.Domain.Tests/
  FootballGamePlanner.Web.Tests/
  FootballGamePlanner.Agents.Tests/
docs/                                  brief, GDPR, agent system, ADRs
.github/workflows/ci.yml               build and tests for every pull request
```

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/FootballGamePlanner.Web
```

## Contribution rules

- `main` is protected: every change goes through a pull request with green CI.
- One branch per story: `story/<issue-number>-<short-name>`.
- Write code, comments, commits, issues, pull requests and documentation in English. Only text shown to users in the app is Swedish.
- Warnings are errors, and NuGet packages with known vulnerabilities fail the build.
- Package versions are set only in `Directory.Packages.props`.
- Use invented test data only, never real personal data (see the GDPR analysis).
