# GDPR-analys – Football Game Planner

> Tidigare arbetsnamn: Lagappen. Analysen är ett underlag, inte juridisk rådgivning.

## Slutsats

Appen kan bli GDPR-kompatibel med låg risk, eftersom den hanterar få och enkla uppgifter. Tre saker beslutas innan bygget börjar, och ett antal krav går in i kravlistan från start.

**Tre grundbeslut**

1. **Vem är personuppgiftsansvarig?** Beslutat: ledaren som skapar laget. Den som driver appen blir personuppgiftsbiträde åt varje sådan ledare, via användarvillkor med biträdesavtal som ledaren godkänner när laget skapas.
2. **Inga hälsouppgifter.** Appen får inte spara *varför* en spelare byts ut (sjukdom eller skada). Sådana uppgifter är känsliga och kräver ett undantag enligt artikel 9.
3. **Inga riktiga personuppgifter i utvecklingen.** Agentsystemet, GitHub och test- och dev-miljöerna får bara använda påhittad testdata.

Låt någon med kunskap om dataskydd granska analysen innan lansering, eftersom ansvaret läggs på enskilda ledare.

## Vilka personuppgifter appen hanterar

Appen behandlar uppgifter om två grupper: ledare, som är vuxna användare, och spelare, som oftast är barn och aldrig loggar in.

| Registrerad | Uppgifter | Källa | Kommentar |
| --- | --- | --- | --- |
| Ledare | Namn, e-post, konto-id från Google/Microsoft | Inloggningen | Endast det inloggningen kräver |
| Spelare och gästspelare | Förnamn/smeknamn, födelseår, nummer, föredragna positioner, gäststatus | Ledaren matar in | Oftast barn: särskilt skyddsvärda |
| Spelare i match | Trupp, position, planerad speltid och byten | Ledaren och algoritmen | Kopplas till spelaren |
| Inbjudningar | Engångslänk, utgångstid, vem som bjöd in | Appen | Raderas när de använts eller gått ut |

Appen sparar inga känsliga personuppgifter enligt artikel 9. Det måste gälla även fritextfält: inga anteckningar om hälsa, skador eller annat känsligt.

## Roller och ansvar

Den som bestämmer varför och hur uppgifterna behandlas är personuppgiftsansvarig. För en idrottsförening finns inget privatundantag. Det är föreningen som måste kunna visa att behandlingen av barnens uppgifter är laglig ([IMY via hurbra.se](https://hurbra.se/gdpr-barnidrott-matchklipp-streaming/)).

| Part | Roll | Vad som krävs |
| --- | --- | --- |
| Föreningen | Ingen formell roll i MVP | Kan bli ansvarig om appen senare används på föreningsnivå |
| Den som driver appen | Personuppgiftsbiträde | Biträdesavtal med varje ansvarig ledare via användarvillkoren, artikel 28 |
| Microsoft Azure | Underbiträde | Microsofts biträdesavtal (DPA); data lagras i EU |
| Ledarna | Den som skapar laget är personuppgiftsansvarig; inbjudna ledare arbetar under samma ansvar | Rättslig grund, information till vårdnadshavare, hantering av förfrågningar; appen förser ledaren med färdiga texter |

Vem som är personuppgiftsansvarig avgörs i praktiken av vem som bestämmer över behandlingen, inte bara av vad villkoren säger. Om ledarna i själva verket agerar inom föreningens verksamhet kan föreningen ändå anses ansvarig. Inget privatundantag gäller för organiserad lagverksamhet, så ledaren får alla skyldigheter enligt GDPR. Appen ska därför göra ansvaret lätt att bära: tydliga villkor, färdig informationstext till vårdnadshavare och inbyggd export och radering.

Ett biträdesavtal ska bland annat reglera behandlingens föremål, varaktighet, art och ändamål, säkerhetsåtgärder och underbiträden ([Legiscope](https://www.legiscope.com/blog/personuppgiftsbitradesavtal-art-28.html)).

## Rättslig grund och barn

Rättslig grund är berättigat intresse (artikel 6.1 f). Ledaren har ett intresse av att planera lagets matcher. Uppgifterna är få, används bara internt av ledarna och delas inte vidare.

- **Varför inte samtycke:** samtycke kan återkallas när som helst. Det kräver vårdnadshavares medverkan för barn och fungerar dåligt för en grundläggande funktion som att planera en match.
- **Avvägningstest:** appen innehåller en färdig, kort avvägning som ledaren godkänner och som visar varför intresset väger tyngre än spelarnas integritetsintresse. IMY har bedömt att civilsamhällesorganisationer kan ha ett berättigat intresse men kräver att avvägningen görs ([IMY:s remissvar](https://www.regeringskansliet.se/contentassets/4d9f563fff4641c5ad063646b62f7838/integritetsskyddsmyndigheten-imy.pdf)).
- **Barn:** barns uppgifter är särskilt skyddsvärda, och informationen ska anpassas så att barn kan förstå den. Det talar för dataminimering, ingen publicering och inga bilder.

Avvägningen håller så länge appen förblir ett internt planeringsverktyg. Den behöver göras om om appen får delning, bilder, statistik eller inloggning för spelare.

## Krav på appen

De här kraven går in i kravlistan och blir acceptanskriterier för agenterna.

| Id | Krav | Grund |
| --- | --- | --- |
| GDPR-01 | Spelare sparas bara med förnamn/smeknamn, födelseår, nummer, positioner och gäststatus | Dataminimering, art. 5.1 c |
| GDPR-02 | Inga fritextfält om spelare; inget fält för orsak till byte eller frånvaro | Inga känsliga uppgifter, art. 9 |
| GDPR-03 | Ledare ser bara lag de är inbjudna till; behörighet kontrolleras i API:t, inte bara i gränssnittet | Säkerhet, art. 32 |
| GDPR-04 | Ledare kan visa, rätta, exportera och radera en spelares uppgifter | Registrerades rättigheter, art. 15–20 |
| GDPR-05 | Radering av en spelare tar bort personen helt; historiska matcher visar "Raderad spelare" | Rätt till radering, art. 17 |
| GDPR-06 | Ingen automatisk radering. Ledare kan radera matcher, spelare och lag; lagringstiden dokumenteras som "så länge laget används" | Lagringsminimering, art. 5.1 e |
| GDPR-07 | Kryptering vid lagring och överföring (HTTPS, krypterad databas) | Säkerhet, art. 32 |
| GDPR-08 | Offline-data på telefonen raderas vid utloggning | Säkerhet, art. 32 |
| GDPR-09 | PDF-export innehåller bara det som behövs för matchen och märks "Internt för ledare" | Dataminimering |
| GDPR-10 | Loggar innehåller inga spelarnamn; bara id:n | Dataminimering, art. 32 |
| GDPR-11 | Integritetspolicy i appen, skriven så att även barn och föräldrar förstår | Information, art. 12–14 |
| GDPR-12 | Endast nödvändiga cookies för inloggning, så ingen cookie-banner krävs | Lagen om elektronisk kommunikation |

Engångslänkar som går ut (redan beslutat) uppfyller kravet på säker åtkomsthantering.

## Molntjänster, inloggning och agentsystemet

Varje tjänst som rör personuppgifter behöver ett biträdesavtal och lagring inom EU. Agentsystemet ska aldrig komma i kontakt med riktiga personuppgifter.

- **Azure:** välj en EU-region, helst Sweden Central, för databas, app och backuper. Microsofts DPA ingår i avtalet. Kontrollera listan över underbiträden.
- **Google/Microsoft-inloggning:** appen tar bara emot namn, e-post och konto-id. Inget annat begärs.
- **GitHub:** repot innehåller bara kod och påhittad testdata. Inga databasdumpar, och hemligheter ligger i GitHub Secrets eller Azure Key Vault.
- **Agentsystemet (Claude API):** agenterna arbetar mot kod, krav och testdata. De får aldrig läsbehörighet till produktionsdatabasen eller produktionsloggar.
- **Förslagsalgoritmen** körs i appen och skickar inga uppgifter till någon AI-tjänst. Det förenklar GDPR-arbetet avsevärt.
- **Övervakning:** om loggverktyg används, till exempel Application Insights, får loggarna inte innehålla spelarnamn (se GDPR-10).

## Dokumentation och rutiner

Det här ska finnas på plats innan riktiga spelare läggs in. Allt är kort och kan tas fram under bygget.

- [x] Beslutat: ledaren som skapar laget är personuppgiftsansvarig
- [ ] Användarvillkor med biträdesavtal, som ledaren godkänner när laget skapas
- [ ] Registerförteckning (artikel 30): en sida om ändamål, uppgifter, mottagare och lagringstid
- [ ] Dokumenterat avvägningstest för berättigat intresse
- [ ] Bedömning av om en konsekvensbedömning (DPIA) krävs. Barn räknas som sårbara registrerade, men behandlingen är liten och utan profilering, så en full DPIA krävs troligen inte. Gör bedömningen med [IMY:s mall](https://www.imy.se/link/e83a5ae689c04fa0ac0111c157e45ab6.aspx) och spara den.
- [ ] Integritetspolicy för appen, plus färdig informationstext som ledaren ger spelare och vårdnadshavare
- [ ] Rutin för förfrågningar om tillgång och radering (svar inom en månad)
- [ ] Incidentrutin: personuppgiftsincidenter anmäls till IMY inom 72 timmar
- [ ] Instruktion till ledare: mata bara in det appen frågar efter, och hantera utskrivna PDF:er varsamt

## Källor

- [IMY:s vägledning om barnidrott, refererad av hurbra.se](https://hurbra.se/gdpr-barnidrott-matchklipp-streaming/)
- [IMY:s remissvar om civilsamhällesorganisationer och berättigat intresse](https://www.regeringskansliet.se/contentassets/4d9f563fff4641c5ad063646b62f7838/integritetsskyddsmyndigheten-imy.pdf)
- [IMY: bedömning av behovet av konsekvensbedömning (mall)](https://www.imy.se/link/e83a5ae689c04fa0ac0111c157e45ab6.aspx)
- [Legiscope: personuppgiftsbiträdesavtal enligt artikel 28](https://www.legiscope.com/blog/personuppgiftsbitradesavtal-art-28.html)
- [Legiscope: GDPR i ideella föreningar](https://www.legiscope.com/blog/gdpr-ideell-forening-medlemmar.html)
