# Entwicklerdokumentation

## 1. Ziel

`FireDepartmentMvp` ist ein MVP für einen Touchmonitor in einer Feuerwehrwache. Die Anwendung verbindet Alarmannahme, Einsatzanwesenheit und Dienst-/Veranstaltungsanwesenheit. Alamos FE2 ist ein externer Alarmgeber; die fachliche Logik der Anwendung bleibt davon getrennt.

## 2. Technologie

- .NET 10 / ASP.NET Core
- Blazor Interactive Server für die Touch-Oberfläche
- Entity Framework Core 10
- SQLite im MVP
- Windows-x64 Self-Contained Publish für den Wachen-PC

Die Anwendung ist aktuell ein einzelnes Webprojekt. Für den MVP ist das bewusst einfach gehalten. Fachlogik liegt in Services und Domain-Modellen und nicht in den Razor-Komponenten, soweit dies der aktuelle Stand erlaubt.

## 3. Verzeichnisstruktur

```text
/
├─ src/
│  └─ FireDepartmentMvp/
│     ├─ Components/
│     │  ├─ Layout/
│     │  └─ Pages/
│     ├─ Data/
│     ├─ Domain/
│     ├─ Services/
│     ├─ wwwroot/
│     ├─ Program.cs
│     └─ appsettings.json
├─ docs/
├─ scripts/
│  ├─ deploy/
│  └─ test/
├─ .gitignore
├─ Directory.Build.props
├─ FireDepartmentMvp.sln
└─ NuGet.Config
```

### Verantwortlichkeiten

`Domain/` enthält Entitäten, Enums und DTOs. `Data/` enthält `AppDbContext` und Seed-Daten. `Services/` kapselt Anwendungsfälle wie Einsatzimport, Anwesenheit und Stammdaten. `Components/Pages/` enthält Touch- und Admin-Oberflächen. Die HTTP-Endpunkte werden derzeit in `Program.cs` definiert.

## 4. Lokale Entwicklung

```powershell
dotnet restore .\FireDepartmentMvp.sln
dotnet build .\FireDepartmentMvp.sln
dotnet run --project .\src\FireDepartmentMvp\FireDepartmentMvp.csproj
```

Beim ersten Start wird die SQLite-Datenbank automatisch über `EnsureCreatedAsync()` erzeugt und mit Testdaten befüllt. Die Datenbankdatei ist lokale Laufzeitinformation und wird durch `.gitignore` nicht versioniert.

Für einen kompletten Neustart der Testdaten die Anwendung beenden und die lokale `firedepartment.db` löschen. Beim nächsten Start werden die Seed-Daten neu angelegt.

## 5. Konfiguration

`src/FireDepartmentMvp/appsettings.json` enthält aktuell die SQLite-Verbindung und den Demo-PIN. Der PIN `1234` ist ausschließlich für den Teststand vorgesehen und darf nicht als produktive Authentifizierung betrachtet werden.

Produktive Geheimnisse gehören nicht ins Repository. Für spätere Konfigurationen sind Umgebungsvariablen, Windows-Konfiguration oder ein Secret Store vorzuziehen.

## 6. Datenmodell

Zentrale Entitäten sind:

- `Activity`: gemeinsamer Vorgang für Einsatz oder Event
- `MissionDetails`: einsatzspezifische Daten und externe Alarm-ID
- `Attendance`: Teilnahme einer Person an einem Vorgang
- `Firefighter` / `FirefighterGroup`: Personen und fünf Gruppen
- `Qualification`: Qualifikationen
- `Vehicle` / `VehicleSeat`: Fahrzeuge und Sitzplätze
- `EventType`: Dienst-/Veranstaltungsarten und Listenlayout

Wichtige Integritätsregeln im EF-Modell:

- Person nur einmal je Vorgang
- Sitzplatz nur einmal je Vorgang
- Qualifikation je Person nur einmal

Stammdaten werden bevorzugt deaktiviert statt gelöscht, damit historische Zuordnungen erhalten bleiben.

## 7. Einsatzimport und Idempotenz

Alle Alarmwege werden auf `ActivityService.ImportMissionAsync()` abgebildet. Ist eine `ExternalId` unbekannt, wird ein Einsatz erzeugt. Ist sie bekannt und der Einsatz offen, werden Alarmdaten aktualisiert. Ist der Einsatz bereits geschlossen, wird er nicht wieder geöffnet.

Damit kann FE2 denselben Alarm bei Nachträgen erneut liefern, ohne doppelte Einsätze zu erzeugen. Alarmgeber sollten deshalb nach Möglichkeit eine stabile externe Einsatz-ID mitsenden.

Die interne Einsatznummer wird pro Jahr fortlaufend aus `MissionDetails.Year` und `Number` gebildet. Für einen echten Mehrinstanzbetrieb müsste die Nummernvergabe transaktionssicherer gestaltet werden.

## 8. HTTP-Schnittstellen

### Health

`GET /api/health`

Dient der Erreichbarkeitsprüfung.

### JSON-Alarm

`POST /api/alarm`

Erwartet beispielsweise:

```json
{
  "externalId": "TEST-001",
  "keyword": "FEU",
  "message": "Gebaeudebrand",
  "street": "Musterstrasse",
  "houseNumber": "12",
  "city": "Musterstadt"
}
```

Der Endpunkt versucht strikt UTF-8. Für ältere Sender existiert ein Kompatibilitäts-Fallback. Ungültiges JSON wird mit HTTP 400 beantwortet.

### FE2 Diagnose

`GET /api/alarm/fe2/debug?...`

Erzeugt keinen Einsatz. Der Endpunkt gibt nur die empfangenen Query-Parameter zurück und dient der Inbetriebnahme des Alamos-Plugins `URL öffnen`.

### FE2 produktiv

`GET /api/alarm/fe2?...`

Akzeptiert die normalisierten Parameter `externalId`, `keyword`, `message`, `street`, `houseNumber`, `city` und optional `alarmTime`. Einige alternative deutsche/FE2-nahe Namen werden ebenfalls akzeptiert.

Details stehen in `docs/fe2-integration.md`.

## 9. Blazor-Zustandsaktualisierung

`AppState` signalisiert Änderungen an der Oberfläche. Services rufen nach fachlichen Änderungen `NotifyChanged()` auf. Die Startseite reagiert darauf und lädt offene Vorgänge neu. Dadurch kann ein Alarm, der über HTTP eintrifft, auf einer bereits geöffneten Touchoberfläche erscheinen.

## 10. SQLite-Besonderheit

EF Core/SQLite kann `DateTimeOffset` in bestimmten `ORDER BY`-Ausdrücken nicht übersetzen. Offene Aktivitäten werden deshalb zunächst geladen und anschließend in .NET nach `StartedAt` sortiert. Bei der geringen Anzahl gleichzeitig offener Vorgänge ist das für den MVP unkritisch.

## 11. Deployment

Das Publish-Skript liegt unter:

```text
scripts/deploy/publish-windows.ps1
```

Es soll eine self-contained `win-x64`-Ausgabe erzeugen. Details und Wachenbetrieb stehen in `docs/station-deployment.md`.

## 12. Entwicklungsregeln

- Änderungen an Fachlogik möglichst in Services, nicht direkt in Razor-Komponenten.
- Keine produktiven PINs, Passwörter, Tokens oder personenbezogenen Echtdaten committen.
- Neue Datenbankregeln im `AppDbContext` dokumentieren.
- Bei Änderungen an FE2-Mapping Diagnose-Endpunkt zuerst testen.
- `externalId` bei Alarmtests immer eindeutig wählen, außer ein Update desselben Einsatzes soll gezielt getestet werden.
- Geschlossene Vorgänge dürfen über Alarmnachträge nicht automatisch wieder geöffnet werden.

## 13. Noch offene technische Arbeiten

Vor Produktivbetrieb sind insbesondere sinnvoll:

- EF-Core-Migrations statt `EnsureCreated`
- automatisierte Unit-/Integrationstests
- echte Authentifizierung und Rollen statt Demo-PIN
- Archiv-/Statistikoberfläche
- Druck/PDF der abgeschlossenen Listen
- Backup- und Restore-Konzept
- Logging/Audit-Trail für relevante Änderungen
- konfigurierbare Datenaufbewahrung und Datenschutzkonzept
- Absicherung der FE2-Schnittstelle und Einschränkung auf das Wachen-/FE2-Netz
- transaktionssichere Einsatznummernvergabe

## 14. Git-Workflow

Empfohlen ist ein kleines Feature-Branch-Modell:

```text
main
feature/fe2-mapping
feature/archive
fix/seat-selection
```

Vor einem Commit mindestens:

```powershell
dotnet build .\FireDepartmentMvp.sln
git status
git diff
```

Commit-Nachrichten sollten den Zweck beschreiben, z. B. `Add FE2 debug endpoint` oder `Fix SQLite activity ordering`.
