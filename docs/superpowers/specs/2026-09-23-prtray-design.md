# PrTray – GitHub PR-varsler i system tray

## Context

Gitify fungerte dårlig (usynlig svart ikon på GNOME, tung Electron-app). Som midlertidig løsning kjører nå bash-tjenesten `~/.local/bin/gh-pr-review-notifier` (systemd user service) som varsler via `notify-send` når egne PR-er blir approved / changes requested. Målet er en liten, OS-agnostisk tray-app som gjør det samme og mer, og som bruker `gh` CLI til all GitHub-kommunikasjon.

**Brukerens krav (sagt):** OS-agnostisk, lite ikon i system tray, bruker `gh`. Tray-meny med mine PR-er + review-forespørsler til meg. Både native meny og et oversiktsvindu. .NET + Avalonia. Kun for meg, lokalt bygg. Varsler for:

1. **PR opprettet** – alle nye PR-er (fra andre) i utvalgte repoer.
2. **Nye commits etter review uten godkjenning** – PR (ikke min egen) der siste review fra et menneske som ikke er forfatteren er *changes requested* eller *commented*, og head-commit har endret seg siden den reviewen. Bot-reviews (f.eks. Copilot) og forfatterens egne reviews ignoreres. Gjelder PR-er jeg har reviewet, er bedt om å reviewe, eller som ligger i utvalgte repoer.
3. **PR godkjent** – alle: mine PR-er, PR-er jeg har reviewet, og PR-er i utvalgte repoer.
4. **PR merged** – samme utvalg som «godkjent».
5. **Endringer bedt om** – på mine PR-er.
6. **Review-forespørsel** – når jeg blir lagt til som reviewer på en PR.

**Antakelser:** Erstatter bash-notifieren. Azure DevOps er utenfor scope. Ingen CI/installere/signering. Autostart settes kun opp på Linux. «Alle» for godkjent/merged tolkes som unionen i punkt 3.

**Suksess:** Appen starter ved innlogging på Ubuntu/GNOME, ikonet er synlig i topplinjen, klikk viser PR-ene med status, og jeg får et klikkbart OS-varsel (åpner PR) innen ~2 min for hver av hendelsene over – uten duplikater og uten flom av gamle hendelser ved første oppstart.

## Design

Solution `~/repos/PrTray`, .NET 10 (SDK 10.0.112), Avalonia 12.1.3 (tray-ikon verifisert i GNOME/Wayland med en probe).

### Konfigurasjon
`~/.config/PrTray/config.json` (`%APPDATA%\PrTray\` på Windows, `~/Library/Application Support/PrTray/` på macOS). Opprettes med standardverdier hvis den mangler:

```json
{ "watchedRepositories": ["acme/widgets"], "pollIntervalSeconds": 120 }
```

### PrTray.Core (net10.0, ingen UI)
- `GhClient` – kjører `gh api graphql -f query=...` via `Process`, parser med System.Text.Json. Én spørring, felles fragment `PrFields` (`id number title url state isDraft createdAt mergedAt author{login} repository{nameWithOwner} reviewDecision commits(last:1){nodes{commit{oid}}} reviews(last:30){nodes{id state submittedAt author{__typename login} commit{oid}}}`), aliaser:
  - `mine`: `is:pr is:open author:@me archived:false`
  - `requested`: `is:pr is:open review-requested:@me archived:false`
  - `reviewed`: `is:pr is:open reviewed-by:@me archived:false`
  - `watched`: `is:pr is:open archived:false repo:A repo:B …` (utelates hvis listen er tom)
  - `mergedMine`, `mergedReviewed`, `mergedWatched`: som over med `is:merged merged:>=<i dag − 2 døgn>`
  - `viewer{login}`
  - Alle feltene er verifisert mot GitHub i denne sesjonen.
  - Returnerer `GhResult`: `Success(PrSnapshot)` | `NotInstalled` | `NotAuthenticated` | `Failed(message)`.
- Modeller (immutable records): `PullRequest` (inkl. `HeadCommitOid`, `Reviews`, `State`, `MergedAt`, `AuthorLogin`, og hvilke grupper den tilhører: `Mine`, `ReviewRequested`, `ReviewedByMe`, `Watched`), `Review`, `PrSnapshot` (viewer-login + PR-er slått sammen på `id`).
- `ChangeDetector` – ren statisk logikk: `(PrSnapshot, IReadOnlySet<string> seen) → DetectionResult(Events, NewSeenKeys)`. Hendelser og dedup-nøkler:

  | Hendelse | Regel | Nøkkel |
  |---|---|---|
  | `Opened` | Watched, forfatter ≠ meg, åpen | `opened:<prId>` |
  | `NewCommitsSinceUnapprovedReview` | Forfatter ≠ meg, siste menneskelige review fra andre enn forfatteren er CHANGES_REQUESTED/COMMENTED, og `HeadCommitOid` ≠ reviewens commit | `recommit:<prId>:<headOid>` |
  | `Approved` | APPROVED-review av andre enn meg | `review:<reviewId>` |
  | `ChangesRequested` | Min PR, CHANGES_REQUESTED av andre | `review:<reviewId>` |
  | `ReviewRequested` | I `requested` | `requested:<prId>:<headOid>` |
  | `Merged` | `State == MERGED` | `merged:<prId>` |

  Hvis samme PR gir `ReviewRequested` sammen med `Opened` eller `NewCommitsSinceUnapprovedReview` i én runde, vises bare `ReviewRequested` (alle nøkler markeres sett). Hendelser eldre enn 24 t markeres sett uten varsel (hindrer flom når et repo legges til).
- `SeenStore` – JSON i `~/.local/state/PrTray/seen.json` (`XDG_STATE_HOME`-fallback), `%LOCALAPPDATA%\PrTray\` på Windows. Manglende/tom fil ⇒ første kjøring registrerer alle nøkler uten å varsle. Korrupt fil ⇒ behandles som første kjøring.
- `Poller` – `PeriodicTimer` (fra config) + `RefreshNowAsync()`; eksponerer `SnapshotUpdated` og `EventsDetected`. Aldri to samtidige kall.

### PrTray.App (Avalonia 12, net10.0)
- `App` med `TrayIcon` + `NativeMenu`, ingen hovedvindu ved oppstart (`ShutdownMode.OnExplicitShutdown`).
- Meny (bygges på nytt ved hvert snapshot): seksjonene «Mine PR-er», «Til review» (requested + nye commits etter review uten godkjenning), «Overvåkede repoer»; hver linje `✅/❌/⏳ repo#nr tittel` → åpner URL. Deretter «Vis oversikt…», «Oppdater nå», deaktivert «Sist oppdatert HH:mm» (+ «(feilet)»), «Avslutt». Feiltilstand: «gh ikke innlogget – kjør gh auth login» / «gh ikke funnet».
- `OverviewWindow` – liste med repo, tittel, status, forfatter, alder; lukk = skjul.
- `TrayIconFactory` – tegner ikonet i kode (ingen PNG-filer): sirkel med lys kontur så den synes på mørk og lys topplinje. Farge: grå (feil), rød (noe krever handling fra meg: changes requested på min PR, review-forespørsel, nye commits etter review uten godkjenning), grønn (min PR godkjent), nøytral ellers.
- `INotifier` – egen implementasjon per OS (DesktopNotifications-pakken avhenger av Avalonia 0.10 og kan ikke brukes):
  - Linux: `notify-send --action=open=Åpne --wait` (verifisert her), klikk åpner PR.
  - macOS: `osascript -e 'display notification …'` (uten klikk-handling).
  - Windows: PowerShell-toast via `Windows.UI.Notifications` (uten klikk-handling).
- `UrlOpener` – `Process.Start` med `UseShellExecute=true`.

### PrTray.Core.Tests (xUnit v3)
- `ChangeDetector`: én test per rad i tabellen, egen review ignoreres, COMMENTED ignoreres, første kjøring gir ingen hendelser, allerede sett gir ingen hendelser, Opened+ReviewRequested-dedup, nye commits etter at jeg deretter har godkjent gir ingen hendelse.
- `GhClient`-parsing mot JSON-fixture lagret fra ekte `gh api graphql`-respons; `GhQueryBuilder` med/uten watched-repoer.
- `SeenStore`/`ConfigStore` round-trip og korrupt fil i temp-mappe.

### Kodestil (fra CLAUDE.md)
Helpers som `static` metoder på klasser, member access fremfor destrukturering, selvforklarende navn, minimalt med kommentarer.

## Verifisering

- `dotnet test` grønt.
- Kjør appen: ikonet synlig i GNOME-topplinjen (også via D-Bus `StatusNotifierWatcher.RegisteredStatusNotifierItems`), menyen viser de faktiske PR-ene.
- Varsel-test: fjern nøkler fra `seen.json` (f.eks. `merged:<id>` for `widgets#72`) → varsel dukker opp, klikk åpner PR.
- Feiltest: `GH_CONFIG_DIR` mot tom mappe → grått ikon + «gh ikke innlogget».
- Etter install-skriptet: logg ut/inn → appen starter automatisk, bash-tjenesten er deaktivert.
