# Product brief – Football Game Planner

> Former working name: Lagappen.

## Vision

Football Game Planner lets football coaches plan matches in advance from their phone: squad, line-up, planned substitutions and fair playing time in one place.

Today this is often done with paper, notes and mental arithmetic on the touchline. As a result, playing time ends up uneven, and it is hard to see afterwards who played what.

## Users and roles

The users are coaches ("leaders"). A team can have several leaders, and a leader can have access to one or more teams.

- **Leader:** signs in with a Google or Microsoft account, creates teams and manages players, matches, line-ups and planned substitutions.
- **Team:** stands alone. No club level in the MVP.
- **Player:** stored with first name or nickname, birth year, shirt number and positions, and can be marked as a guest player, but does not sign in in the MVP.

New leaders get access through a personal one-time invite link that expires after a while (proposal: 7 days). All leaders have the same rights. If two leaders edit the same match at the same time, the one who saves last is warned and chooses what to keep.

## MVP scope

The first version covers planning before a match only, from team to line-up with planned substitutions. Nothing is recorded during a match. Created matches and their substitution plans are saved, and a leader can open and change them until match day.

| Area | Feature |
| --- | --- |
| Account | Sign in with Google/Microsoft |
| Team | Create a team, invite more leaders through an invite link, switch between teams |
| Players | Add players and guest players with first name/nickname, birth year, shirt number and preferred positions |
| Game formats | Built-in formats (5, 7, 9 and 11 a side) plus custom ones; built-in and custom formations per format |
| Match | Create a match with opponent, date (required), time, venue, periods, playing time and breaks (pick a previously used setup or create a new one; the most recently used is preselected); in the same flow, choose which squad players take part (all preselected) and add guest players, previous or new |
| Line-up | Place players in positions, as a list and on a graphic pitch; bench |
| Substitutions | Plan substitutions and playing time per player |
| Planning | When creating a match, the leader chooses between a suggested substitution schedule and fully manual planning; a suggestion can also be requested later |

## Game formats, substitutions and playing time

The core of the app is playing-time planning: the leader plans, but can ask for a suggestion with fair playing time.

**Game formats**

- Built-in formats: 5, 7, 9 and 11 a side.
- Custom formats, where the leader sets the number of players. Each format has formations with positions placed on the pitch, both built-in and custom.

**Substitutions and playing time**

- The app shows planned playing time per player and position.
- Before a match, the leader can plan playing time manually.

**Line-up suggestion**

- Input: squad, formation, periods, minutes per period, breaks and number of substitutions per period.
- Option: whether to use the players' preferred positions.
- Output: a starting line-up plus a substitution schedule where playing time is as equal as possible within the match, given the number of substitutions.
- The leader can adjust any plan manually, suggested or their own: swap two players in the same substitution window, on the pitch or between pitch and bench. Playing time recalculates at once, edited cells are marked and the suggestion can be restored.

**Rules**

- Substitutions happen only in fixed substitution windows: the leader sets the number of substitutions per period.
- The leader picks the goalkeeper manually for each period; the algorithm distributes the outfield players.
- The suggestion is computed by a deterministic algorithm, not an AI model. The same input always gives the same suggestion.
- A player is either in the squad or not.

**Preferred positions**

- A player has one or more preferred areas: GK, Back, Midfield or Attack. All carry equal weight, with no ranking.
- Every position in a formation belongs to an area, also in custom formations, where the leader sets the area per position.
- Fair playing time comes before preferred positions. The suggestion follows the positions as far as possible, and placements outside a player's areas are flagged.

**Changing the squad after the match is created**

- The leader can replace a player in the squad, for example because of illness or injury. The leader then chooses whether the replacement takes over position and schedule, or the suggestion is recalculated. The reason is never stored.
- The leader can add a borrowed player, for example from a younger or older age group, manually to the match. The player is saved in the team as a guest player and can be reused in later matches.

**On match day**

- Match view on the phone with line-up and substitution schedule.
- PDF export of the match plan, with line-up, substitution schedule and planned playing time, so the leader can print it and keep the phone in the bag. The leader picks the format at export: compact, list only, or with a graphic pitch.
- The plan can be viewed offline once it has been loaded.

## Out of scope for the MVP

These wait for later versions:

- Club level with several teams and shared players
- Sign-in for players and parents, call-ups and sign-ups
- Native mobile app
- Match statistics beyond playing time (goals, assists)
- Training sessions and attendance
- Recording substitutions and actual playing time during a match
- Sharing with players and parents
- Fair playing time across several matches
- Partial availability, for example maximum minutes for a player
- Drag-and-drop on the graphic pitch; in the MVP the leader swaps players by tapping two cells

## Technology and SDLC

Everything is built in C#/.NET, with code on GitHub and hosting in Azure. An agent system drives the work from requirements to deployment.

| Part | Choice |
| --- | --- |
| Web app | ASP.NET Core with Blazor, mobile-first |
| Database | Azure SQL Database |
| Sign-in | Google/Microsoft via Microsoft Entra External ID (to be verified) |
| Hosting | Azure App Service or Container Apps |
| CI/CD | GitHub Actions, environments dev, test and prod |
| Agent system | C# console app on Anthropic's official C# SDK |

The agents work against the repository: every step leaves a file or a pull request. The user approves at three checkpoints: requirements, design and deployment to prod. Failing tests or a rejected review send the work back to the developer agent. See [agent-system.md](agent-system.md).

Scale in the first year: the user's own team or club, tens of users. Hosting is sized for minimal cost. Offline support requires the web app to be built as a PWA. Visual profile: "Strålkastare" (Floodlight: dark, neon mint accent, Sora typeface), but all colours and fonts live as design tokens in one place, so the profile can be swapped later without changing components. The PDF export is always light, for printing.

## Decisions

- [x] Success criterion: a match is planned in under 5 minutes
- [x] Swedish only in the MVP, prepared for more languages; the app's name is English
- [x] The leader who creates the team is the data controller; matches are never deleted automatically
- [x] Any leader can delete the team after a confirmation
- [x] Saved playing-time setups are shared by the team's leaders; the whole squad is preselected when a match is created
- [x] The app is called Football Game Planner; the repository is JWeiderberg/FootballGamePlanner on GitHub
- [x] Everything in the repository (code, documentation, issues, pull requests) is in English; only the app's user interface is Swedish
