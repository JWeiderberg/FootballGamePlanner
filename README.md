# Football Game Planner

Matchplanering för fotbollsledare: trupp, uppställning, planerade byten och jämn speltid, från mobilen.

Appen byggs story för story av ett AI-agentsystem med kontrollpunkter där en människa godkänner. Se [docs/agentsystem.md](docs/agentsystem.md).

## Dokument

| Dokument | Innehåll |
| --- | --- |
| [docs/brief.md](docs/brief.md) | Produktbrief: vision, MVP-omfång, regler för byten och speltid |
| [docs/gdpr.md](docs/gdpr.md) | GDPR-analys och kraven GDPR-01 till 12 |
| [docs/agentsystem.md](docs/agentsystem.md) | Agentsystemet: agenter, kontrollpunkter, behörigheter, teststrategi |
| [docs/adr/](docs/adr/) | Arkitekturbeslut |

Dokumenten i `docs/` är dem agenterna läser och utgår från.

## Struktur

```
src/
  FootballGamePlanner.Web/             Blazor-webbapp (mobilanpassad, blir PWA)
  FootballGamePlanner.Domain/          domänmodell och förslagsalgoritmen
  FootballGamePlanner.Infrastructure/  databas och inloggning
tests/
  FootballGamePlanner.Domain.Tests/
  FootballGamePlanner.Web.Tests/
docs/                                  brief, GDPR, agentsystem, ADR:er
.github/workflows/ci.yml               bygge och tester för varje pull request
```

## Kom igång

Kräver [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/FootballGamePlanner.Web
```

## Regler för bidrag

- `main` är skyddad: alla ändringar går via pull request med grön CI.
- En branch per story: `story/<issue-nummer>-<kort-namn>`.
- Varningar räknas som fel, och NuGet-paket med kända sårbarheter stoppar bygget.
- Paketversioner sätts bara i `Directory.Packages.props`.
- Bara påhittad testdata, aldrig riktiga personuppgifter (se GDPR-analysen).
