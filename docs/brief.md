# Produktbrief – Football Game Planner

> Tidigare arbetsnamn: Lagappen. Dokumentet är skrivet på svenska.

## Vision

Football Game Planner låter fotbollsledare planera matcher i förväg från mobilen: trupp, uppställning, planerade byten och jämn speltid på ett ställe.

I dag sköts detta ofta med papper, anteckningar och huvudräkning vid sidlinjen. Därför blir speltiden ojämn och det är svårt att i efterhand se vem som spelat vad.

## Användare och roller

Användarna är ledare. Ett lag kan ha flera ledare, och en ledare kan ha tillgång till ett eller flera lag.

- **Ledare:** loggar in med Google- eller Microsoft-konto, skapar lag och hanterar spelare, matcher, uppställningar och planerade byten.
- **Lag:** står var för sig. Ingen föreningsnivå i MVP.
- **Spelare:** sparas med förnamn eller smeknamn, födelseår, nummer och positioner, och kan markeras som gästspelare, men loggar inte in i MVP.

Nya ledare får tillgång via en personlig engångslänk som utgår efter en tid (förslag: 7 dagar). Alla ledare har samma behörighet. Ändrar två ledare samma match samtidigt varnas den som sparar sist och får välja.

## MVP-omfång

Första versionen täcker bara planeringen före match, från lag till uppställning med planerade byten. Inget registreras under pågående match. Skapade matcher och deras bytesplaner sparas, och ledaren kan öppna och ändra dem fram till matchdagen.

| Område | Funktion |
| --- | --- |
| Konto | Inloggning med Google/Microsoft |
| Lag | Skapa lag, bjuda in fler ledare via inbjudningslänk, växla mellan lag |
| Spelare | Lägga till spelare och gästspelare med förnamn/smeknamn, födelseår, nummer och föredragna positioner |
| Spelformer | Färdiga format (5, 7, 9 och 11 mot 11) samt egna; färdiga och egna formationer per spelform |
| Match | Skapa match med motståndare, datum (obligatoriskt), tid, plats, perioder, speltid och pauser (välj ett tidigare använt upplägg eller skapa nytt; det senast använda föreslås); i samma flöde välja vilka spelare ur truppen som är med (alla förvalda) och ta in gästspelare, tidigare eller nya |
| Uppställning | Placera spelare på positioner, som lista och på grafisk plan; bänk |
| Byten | Planera byten och speltid per spelare |
| Planering | När matchen skapas väljer ledaren mellan föreslaget bytesschema och helt manuell planering; ett förslag kan också tas fram senare |

## Spelformer, byten och speltid

Kärnan i appen är speltidsplaneringen: ledaren planerar själv, men kan be om ett förslag med jämn speltid.

**Spelformer**

- Färdiga spelformer: 5, 7, 9 och 11 mot 11.
- Egna spelformer, där ledaren anger antal spelare. Varje spelform har formationer med positioner placerade på planen, både färdiga och egna.

**Byten och speltid**

- Appen visar planerad speltid per spelare och position.
- Före match kan ledaren planera speltiden manuellt.

**Förslag på uppställning**

- Input: trupp, formation, perioder, speltid per period, pauser och antal byten per period.
- Val: om spelarnas föredragna positioner ska användas.
- Output: startuppställning plus bytesschema där speltiden blir så lika som möjligt inom matchen, givet antalet byten.
- Ledaren kan justera bytesplanen manuellt, både ett förslag och en egen plan: byta plats mellan två spelare i samma bytesfönster, eller mellan plan och bänk. Speltiden räknas om direkt, ändrade rutor markeras och förslaget kan återställas.

**Regler**

- Byten sker bara i fasta bytesfönster: ledaren anger antal byten per period.
- Ledaren väljer målvakt manuellt per period; algoritmen fördelar utespelarna.
- Förslaget räknas fram av en deterministisk algoritm, inte en AI-modell. Samma input ger samma förslag.
- En spelare är antingen med i truppen eller inte.

**Föredragna positioner**

- En spelare har ett eller flera föredragna områden: MV, Back, Mitt eller Anfall. Alla väger lika, utan inbördes ordning.
- Varje position i en formation hör till ett område, även i egna formationer, där ledaren anger området per position.
- Jämn speltid går före föredragna positioner. Förslaget följer positionerna så långt det går, och placeringar utanför en spelares områden markeras.

**Ändra trupp efter att matchen skapats**

- Ledaren kan byta ut en spelare i truppen, till exempel vid sjukdom eller skada. Ledaren väljer då om ersättaren tar över position och bytesschema, eller om förslaget räknas om. Orsaken sparas aldrig.
- Ledaren kan lägga till en inlånad spelare, till exempel från en yngre eller äldre årskull, manuellt i matchen. Spelaren sparas i laget som gästspelare och kan återanvändas i senare matcher.

**På matchdagen**

- Matchvy i mobilen med uppställning och bytesschema.
- Export av matchens planering till PDF, med uppställning, bytesschema och planerad speltid, så att ledaren kan skriva ut den och slipper telefonen under matchen. Ledaren väljer format vid export: kompakt med enbart lista, eller med grafisk visualisering på plan.
- Planen går att visa offline när den väl är laddad.

## Utanför MVP

Dessa väntar till senare versioner:

- Föreningsnivå med flera lag och delade spelare
- Inloggning för spelare och föräldrar, kallelser och anmälan
- Native mobilapp
- Matchstatistik utöver speltid (mål, assist)
- Träningar och närvaro
- Registrera byten och logga faktisk speltid under pågående match
- Delning med spelare och föräldrar
- Jämn speltid över flera matcher
- Begränsad tillgänglighet, till exempel max speltid för en spelare
- Dra-och-släpp på den grafiska planen; i MVP byter ledaren plats genom att trycka på två rutor

## Teknik och SDLC

Allt byggs i C#/.NET, med kod på GitHub och drift i Azure. Ett agentsystem driver arbetet från krav till deploy.

| Del | Förslag |
| --- | --- |
| Webbapp | ASP.NET Core med Blazor, mobilanpassad |
| Databas | Azure SQL Database |
| Inloggning | Google/Microsoft via Microsoft Entra External ID (att verifiera) |
| Hosting | Azure App Service eller Container Apps |
| CI/CD | GitHub Actions, miljöerna dev, test och prod |
| Agentsystem | C#-konsolapp på Anthropics officiella C#-SDK |

Agenterna arbetar mot repot: varje steg lämnar en fil eller en pull request. Användaren godkänner vid tre kontrollpunkter: krav, design och deploy till prod. Underkända tester eller granskning skickar arbetet tillbaka till utvecklar-agenten. Se [agentsystem.md](agentsystem.md).

Skala första året: det egna laget eller klubben, tiotals användare. Driften dimensioneras för minimal kostnad. Offline-stödet kräver att webbappen byggs som PWA. Grafisk profil: Strålkastare (mörk, neonmint, typsnittet Sora), men alla färger och typsnitt läggs som designtokens på ett ställe så att profilen kan bytas senare utan kodändringar i komponenterna. PDF-exporten är alltid ljus för utskrift.

## Beslut

- [x] Framgångskriteriet är att en match planeras på under 5 minuter
- [x] Bara svenska i MVP, förberett för fler språk; appens namn är engelskt
- [x] Personuppgiftsansvarig är ledaren som skapar laget; ingen automatisk radering av matcher
- [x] Alla ledare kan radera laget efter en bekräftelse
- [x] Sparade upplägg delas av lagets ledare; hela truppen är förvald när en match skapas
- [x] Appen heter Football Game Planner; repot är JWeiderberg/FootballGamePlanner på GitHub
