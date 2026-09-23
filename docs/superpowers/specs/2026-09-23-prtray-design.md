# PrTray – GitHub PR-varsler i system tray

## Context

Gitify fungerte dårlig (usynlig svart ikon på GNOME, tung Electron-app). Som midlertidig løsning kjører nå bash-tjenesten `~/.local/bin/gh-pr-review-notifier` (systemd user service) som varsler via `notify-send` når egne PR-er blir approved / changes requested. Målet er en liten, OS-agnostisk tray-app som gjør det samme og mer, og som bruker `gh` CLI til all GitHub-kommunikasjon.

**Brukerens krav (sagt):** OS-agnostisk, lite ikon i system tray, bruker `gh`. Varsle når min PR blir approved eller ikke. Tray-meny med mine PR-er + review-forespørsler til meg. Både native meny og et oversiktsvindu. .NET + Avalonia. Kun for meg, lokalt bygg.

**Antakelser:** Erstatter bash-notifieren. Azure DevOps er utenfor scope. Ingen CI/installere/signering. Autostart settes kun opp på Linux.

**Suksess:** Appen starter ved innlogging på Ubuntu/GNOME, ikonet er synlig i topplinjen, klikk viser mine åpne PR-er og review-forespørsler med status, og jeg får OS-varsel (klikkbart → åpner PR) innen ~2 min når noen approver / ber om endringer på min PR, eller ber meg om review.

## Design

Ny solution `~/repos/PrTray` (git init, .NET 10 – SDK 10.0.112 er installert).

### PrTray.Core (net10.0, ingen UI)
- `GhClient` – kjører `gh api graphql -f query=...` via `Process`, parser med System.Text.Json. Én spørring med to aliaser:
  - `mine: search(query:"is:pr is:open author:@me archived:false", type:ISSUE, first:50)`
  - `requested: search(query:"is:pr is:open review-requested:@me archived:false", type:ISSUE, first:50)`
  - Felter: `number title url isDraft updatedAt repository{nameWithOwner} reviewDecision reviews(last:30){nodes{id state submittedAt author{login}}}` + `viewer{login}`.
  - Query-en er allerede verifisert fra shell i denne sesjonen (samme form som bash-skriptet).
  - Returnerer `GhResult` (Success(snapshot) | NotInstalled | NotAuthenticated | Failed(message)). Skiller ved exit-kode/stderr (`gh auth status`).
- `PullRequest`, `Review`, `PrSnapshot` – immutable records.
- `ChangeDetector` – ren statisk logikk: (snapshot, seen) → liste av `PrEvent` (`Approved`, `ChangesRequested`, `ReviewRequested`) + oppdatert seen-sett. Filtrerer bort egne reviews og `COMMENTED`. Nøkkel: review-id for reviews, `repo#nr` + updatedAt for review-forespørsler.
- `SeenStore` – JSON-fil i `~/.local/state/PrTray/seen.json` (Linux/macOS: `XDG_STATE_HOME` fallback), `%LOCALAPPDATA%\PrTray\` på Windows. Tom fil ⇒ første kjøring registrerer uten å varsle.
- `Poller` – `PeriodicTimer` (120 s) + `RefreshNowAsync()`; eksponerer `SnapshotUpdated` og `EventsDetected`.

### PrTray.App (Avalonia, net10.0)
- `App.axaml` med `TrayIcon` + `NativeMenu`, ingen hovedvindu ved oppstart (`ShutdownMode.OnExplicitShutdown`).
- Meny (bygges på nytt ved hvert snapshot):
  - «Mine PR-er» → `✅/❌/⏳ repo#nr tittel` → åpner URL
  - «Review-forespørsler» → samme format
  - separator, «Vis oversikt…», «Oppdater nå», deaktivert «Sist oppdatert HH:mm» (+ «(feilet)»), «Avslutt»
  - Feiltilstand: «gh ikke innlogget – kjør `gh auth login`» / «gh ikke funnet».
- `OverviewWindow` – liste med repo, tittel, status, reviewers, alder; lukk = skjul.
- `TrayIconRenderer` – velger ikon etter tilstand: nøytral, grønn (approved finnes), rød (changes requested), blå prikk (review-forespørsel), grå (feil). PNG-er med lys kontur så de synes på mørk og lys topplinje.
- `INotifier` + implementasjon via NuGet `DesktopNotifications` (FreeDesktop D-Bus / Windows toast), `osascript`-fallback på macOS. Klikk på varsel åpner PR-URL.
- `UrlOpener` – `Process.Start` med `UseShellExecute=true` (xdg-open/open/start).

### PrTray.Core.Tests (xUnit)
- `ChangeDetector`: første kjøring, ny approve, ny changes requested, egen review ignoreres, COMMENTED ignoreres, ny review-forespørsel, allerede sett.
- `GhClient`-parsing mot JSON-fixtures (lagres fra en ekte `gh api graphql`-respons, anonymiseres ikke nødvendig – egne data).
- `SeenStore` round-trip i temp-mappe.

### Kodestil (fra CLAUDE.md)
Helpers som `static` metoder på klasser, member access fremfor destrukturering, selvforklarende navn, minimalt med kommentarer.

## Implementeringssteg

1. Opprett `~/repos/PrTray`, `git init`, solution + tre prosjekter, `.gitignore`. Lagre denne specen som `docs/superpowers/specs/2026-09-23-prtray-design.md` og commit.
2. Core: modeller + `ChangeDetector` med tester først (TDD).
3. Core: `GhClient` + fixture-basert parsing-test; lag fixture fra ekte `gh`-kall.
4. Core: `SeenStore` + `Poller`.
5. App: Avalonia-skall med `TrayIcon`, statisk meny, Avslutt – verifiser at ikonet vises i GNOME-topplinjen.
6. App: koble Poller → dynamisk meny, ikon-tilstander, `UrlOpener`.
7. App: `INotifier` med DesktopNotifications, klikk-for-å-åpne.
8. App: `OverviewWindow`.
9. `scripts/install-linux.sh`: `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true` til `~/.local/share/PrTray/`, skriv `~/.local/share/applications/prtray.desktop` + `~/.config/autostart/prtray.desktop`, og `systemctl --user disable --now gh-pr-review-notifier.service`.

## Verifisering

- `dotnet test` grønt.
- Kjør appen: ikonet synlig i GNOME-topplinjen (sjekk også via D-Bus `StatusNotifierWatcher.RegisteredStatusNotifierItems`), menyen viser de faktiske åpne PR-ene (i dag: `acme/widgets#72`, `acme/sms-gateway#1`, `example/todo-list#1`).
- Varsel-test: slett en review-id fra `seen.json` for en PR med approve (eller bruk en test-PR i et eget repo og approve fra en annen konto/be en kollega) → varsel dukker opp, klikk åpner PR.
- Feiltest: kjør med `GH_CONFIG_DIR` pekende på tom mappe → grått ikon + «gh ikke innlogget».
- Etter install-skriptet: logg ut/inn → appen starter automatisk, bash-tjenesten er deaktivert.
