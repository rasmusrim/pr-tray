# PrTray

Et lite tray-ikon som holder deg oppdatert på GitHub-PR-ene dine. PrTray bruker [GitHub CLI](https://cli.github.com/) (`gh`) til all kommunikasjon med GitHub, så appen håndterer aldri tokens selv.

Du får skrivebordsvarsler, og et klikk på varselet åpner PR-en, når

- en ny PR opprettes i et repo du overvåker
- en PR får nye commits etter en review som ikke var en godkjenning
- en PR blir godkjent
- noen ber om endringer på PR-en din
- du blir bedt om review
- en PR blir merget

Tray-menyen viser «Mine PR-er», «Til review» og «Overvåkede repoer». «Vis oversikt…» åpner et vindu med alle PR-ene, og «Innstillinger…» lar deg velge hvilke repoer som skal være med.

## Krav

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- GitHub CLI, innlogget:
  ```bash
  sudo apt install gh
  gh auth login
  ```
- Ubuntu med GNOME. Tray-ikoner krever utvidelsen **Ubuntu AppIndicators**, som er på som standard. Se [Feilsøking](#feilsøking) hvis ikonet ikke vises.

## Bygge og teste

```bash
dotnet build
dotnet test
```

Kjøre appen rett fra kildekoden:

```bash
dotnet run --project src/PrTray.App
```

## Installere på Ubuntu

```bash
./scripts/install-linux.sh
```

Skriptet

1. stopper en eventuell kjørende PrTray,
2. bygger en selvstendig binær (`dotnet publish`, linux-x64, én fil) til `~/.local/share/PrTray/`,
3. legger PrTray i appmenyen (`~/.local/share/applications/prtray.desktop`),
4. setter opp **automatisk start ved innlogging** (`~/.config/autostart/prtray.desktop`),
5. starter appen.

Etter installasjonen ligger ikonet i topplinjen og starter av seg selv hver gang du logger inn. Det kjører alltid bare én PrTray om gangen.

### Oppdatere

```bash
git pull
./scripts/install-linux.sh
```

### Slå av automatisk start

```bash
rm ~/.config/autostart/prtray.desktop
```

### Avinstallere

```bash
pkill -x prtray
rm -rf ~/.local/share/PrTray
rm ~/.local/share/applications/prtray.desktop ~/.config/autostart/prtray.desktop
rm -rf ~/.config/PrTray ~/.local/state/PrTray   # innstillinger og hva du allerede er varslet om
```

## Innstillinger

Velg **Innstillinger…** i tray-menyen for å legge til eller fjerne repoer. Du kan skrive `eier/repo` eller lime inn en GitHub-lenke. PrTray sjekker at repoet finnes før det legges til.

- **Repoer i listen:** Bare PR-er i disse repoene vises og varsles. De samme repoene er dine «overvåkede repoer», så du får også varsel om nye PR-er, godkjenninger og merger i dem.
- **Tom liste:** Alle repoer er med, men ingen overvåkes. Du får fortsatt varsler om dine egne PR-er og om review-forespørsler.

Innstillingene lagres i `~/.config/PrTray/config.json`:

```json
{
  "repositories": ["eier/repo"],
  "pollIntervalSeconds": 120,
  "ghPath": "gh"
}
```

| Felt | Betydning |
|---|---|
| `repositories` | Repoene som filtreres på og overvåkes |
| `pollIntervalSeconds` | Hvor ofte GitHub sjekkes (minst 30 sekunder) |
| `ghPath` | Stien til `gh`, hvis den ikke ligger i `PATH` |

Hvilke hendelser du allerede er varslet om, lagres i `~/.local/state/PrTray/seen.json`. Første gang appen kjører, registreres alt som finnes uten varsler. Nye PR-er, reviews og merger som er eldre enn et døgn, varsles ikke.

## Feilsøking

**Ikonet vises ikke i topplinjen.** Sjekk at AppIndicator-utvidelsen er på. Andre programmer kan slå den av, for eksempel installasjon av Citrix Workspace:

```bash
gnome-extensions info ubuntu-appindicators@ubuntu.com
gnome-extensions enable ubuntu-appindicators@ubuntu.com
```

**Menyen sier «gh ikke innlogget».** Kjør `gh auth login`.

**Menyen sier «gh ikke funnet».** Installer GitHub CLI, eller sett `ghPath` i `config.json`.

**Et repo mangler.** Når repo-listen ikke er tom, vises bare PR-er fra repoene i listen. Legg repoet til under **Innstillinger…**.

## Andre operativsystemer

Appen bygger og kjører på Windows og macOS også. Der finnes ingen installasjonsskript eller automatisk start, og et klikk på et varsel gjør ingenting.
