# GDPR analysis – Football Game Planner

> Former working name: Lagappen. This analysis is a basis for decisions, not legal advice. Several sources are Swedish, because the app is operated in Sweden and the supervisory authority is IMY (Integritetsskyddsmyndigheten).

## Conclusion

The app can be GDPR compliant at low risk, because it handles few and simple data. Three things are decided before building starts, and a set of requirements goes into the backlog from the start.

**Three founding decisions**

1. **Who is the data controller?** Decided: the leader who creates the team. Whoever operates the app becomes the data processor for each such leader, through terms of service with a data processing agreement that the leader accepts when the team is created.
2. **No health data.** The app must not store *why* a player is replaced (illness or injury). Such data is sensitive and requires an exemption under Article 9.
3. **No real personal data in development.** The agent system, GitHub and the test and dev environments use invented test data only.

Have someone with data protection expertise review the analysis before launch, since responsibility is placed on individual leaders.

## What personal data the app handles

The app processes data about two groups: leaders, who are adult users, and players, who are mostly children and never sign in.

| Data subject | Data | Source | Comment |
| --- | --- | --- | --- |
| Leaders | Name, email, account id from Google/Microsoft | Sign-in | Only what sign-in requires |
| Players and guest players | First name/nickname, birth year, shirt number, preferred positions, guest flag | Entered by the leader | Mostly children: need extra protection |
| Players in a match | Squad, position, planned playing time and substitutions | The leader and the algorithm | Linked to the player |
| Invites | One-time link, expiry time, who sent the invite | The app | Deleted once used or expired |

The app stores no sensitive personal data under Article 9. That must hold for free-text fields too: no notes about health, injuries or anything else sensitive.

## Roles and responsibilities

Whoever decides why and how the data is processed is the data controller. A sports club has no private-use exemption. It is the club that must be able to show that processing children's data is lawful ([IMY via hurbra.se](https://hurbra.se/gdpr-barnidrott-matchklipp-streaming/), in Swedish).

| Party | Role | What is required |
| --- | --- | --- |
| The club | No formal role in the MVP | May become controller if the app is later used at club level |
| The app operator | Data processor | Data processing agreement with each controlling leader through the terms of service, Article 28 |
| Microsoft Azure | Sub-processor | Microsoft's data processing addendum (DPA); data stored in the EU |
| Leaders | The leader who creates the team is the controller; invited leaders work under the same responsibility | Legal basis, information to guardians, handling requests; the app gives leaders ready-made texts |

Who the controller is depends in practice on who decides over the processing, not only on what the terms say. If leaders in fact act within the club's activities, the club may still be considered the controller. No private-use exemption applies to organised team activities, so the leader carries all obligations under the GDPR. The app should therefore make that responsibility easy to carry: clear terms, ready-made information text for guardians, and built-in export and deletion.

A data processing agreement must cover, among other things, the subject matter, duration, nature and purpose of the processing, security measures and sub-processors ([Legiscope](https://www.legiscope.com/blog/personuppgiftsbitradesavtal-art-28.html), in Swedish).

## Legal basis and children

The legal basis is legitimate interest (Article 6(1)(f)). The leader has an interest in planning the team's matches. The data is minimal, used only internally by the leaders and not shared further.

- **Why not consent:** consent can be withdrawn at any time. It requires the involvement of guardians for children and works poorly for a basic function such as planning a match.
- **Balancing test:** the app contains a short, ready-made balancing test that the leader accepts, showing why the interest outweighs the players' privacy interest. IMY has found that civil society organisations can have a legitimate interest but requires the balancing to be done ([IMY's consultation response](https://www.regeringskansliet.se/contentassets/4d9f563fff4641c5ad063646b62f7838/integritetsskyddsmyndigheten-imy.pdf), in Swedish).
- **Children:** children's data needs extra protection, and information must be adapted so that children can understand it. That argues for data minimisation, no publication and no photos.

The balancing holds as long as the app remains an internal planning tool. It must be redone if the app gains sharing, photos, statistics or sign-in for players.

## Requirements on the app

These requirements go into the backlog and become acceptance criteria for the agents.

| Id | Requirement | Basis |
| --- | --- | --- |
| GDPR-01 | Players are stored only with first name/nickname, birth year, shirt number, positions and guest flag | Data minimisation, Art. 5(1)(c) |
| GDPR-02 | No free-text fields about players; no field for the reason for a substitution or absence | No sensitive data, Art. 9 |
| GDPR-03 | Leaders see only teams they have been invited to; access is checked in the API, not only in the UI | Security, Art. 32 |
| GDPR-04 | Leaders can view, correct, export and delete a player's data | Data subject rights, Art. 15–20 |
| GDPR-05 | Deleting a player removes the person completely; past matches show "Deleted player" | Right to erasure, Art. 17 |
| GDPR-06 | No automatic deletion. Leaders can delete matches, players and teams; the retention period is documented as "as long as the team is in use" | Storage limitation, Art. 5(1)(e) |
| GDPR-07 | Encryption at rest and in transit (HTTPS, encrypted database) | Security, Art. 32 |
| GDPR-08 | Offline data on the phone is cleared on sign-out | Security, Art. 32 |
| GDPR-09 | The PDF export contains only what the match needs and is marked "Internal for leaders" | Data minimisation |
| GDPR-10 | Logs contain no player names, only ids | Data minimisation, Art. 32 |
| GDPR-11 | A privacy policy in the app, written so that children and parents can understand it | Information, Art. 12–14 |
| GDPR-12 | Only strictly necessary cookies for sign-in, so no cookie banner is needed | Swedish Electronic Communications Act |

One-time invite links that expire (already decided) meet the requirement for secure access management.

## Cloud services, sign-in and the agent system

Every service that touches personal data needs a data processing agreement and storage within the EU. The agent system must never come into contact with real personal data.

- **Azure:** choose an EU region, preferably Sweden Central, for database, app and backups. Microsoft's DPA is part of the agreement. Check the list of sub-processors.
- **Google/Microsoft sign-in:** the app receives only name, email and account id. Nothing else is requested.
- **GitHub:** the repository contains only code and invented test data. No database dumps; secrets live in GitHub Secrets or Azure Key Vault.
- **Agent system (Claude API):** the agents work with code, requirements and test data. They never get read access to the production database or production logs.
- **The suggestion algorithm** runs inside the app and sends no data to any AI service. That simplifies GDPR work considerably.
- **Monitoring:** if a logging tool is used, for example Application Insights, logs must not contain player names (see GDPR-10).

## Documentation and routines

These must be in place before real players are entered. Each is short and can be produced during the build.

- [x] Decided: the leader who creates the team is the data controller
- [ ] Terms of service with a data processing agreement, accepted by the leader when the team is created
- [ ] Record of processing activities (Article 30): one page on purposes, data, recipients and retention
- [ ] Documented balancing test for legitimate interest
- [ ] Assessment of whether a data protection impact assessment (DPIA) is required. Children count as vulnerable data subjects, but the processing is small and involves no profiling, so a full DPIA is probably not required. Make the assessment with [IMY's template](https://www.imy.se/link/e83a5ae689c04fa0ac0111c157e45ab6.aspx) (in Swedish) and keep it.
- [ ] Privacy policy for the app, plus ready-made information text that leaders give to players and guardians
- [ ] Routine for access and deletion requests (answered within one month)
- [ ] Incident routine: personal data breaches are reported to IMY within 72 hours
- [ ] Instructions for leaders: enter only what the app asks for, and handle printed PDFs with care

## Sources

All sources are in Swedish.

- [IMY guidance on children's sports, referenced by hurbra.se](https://hurbra.se/gdpr-barnidrott-matchklipp-streaming/)
- [IMY's consultation response on civil society organisations and legitimate interest](https://www.regeringskansliet.se/contentassets/4d9f563fff4641c5ad063646b62f7838/integritetsskyddsmyndigheten-imy.pdf)
- [IMY: assessing whether an impact assessment is needed (template)](https://www.imy.se/link/e83a5ae689c04fa0ac0111c157e45ab6.aspx)
- [Legiscope: data processing agreements under Article 28](https://www.legiscope.com/blog/personuppgiftsbitradesavtal-art-28.html)
- [Legiscope: GDPR in non-profit associations](https://www.legiscope.com/blog/gdpr-ideell-forening-medlemmar.html)
