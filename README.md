# Feuerwehr Einsatz & Anwesenheit

Touch-optimierte Webanwendung für die Feuerwehrwache. Die Anwendung nimmt Alarme aus **Alamos FE2** entgegen, zeigt parallele Einsätze an und erfasst Einsatz- sowie Dienstanwesenheiten.

> Status: MVP / Testbetrieb. Nicht als alleinige Alarmierungs- oder Einsatzführungssoftware vorgesehen.

## Funktionen

- Alarmannahme über REST (`POST /api/alarm`) und Alamos FE2 (`GET /api/alarm/fe2`)
- FE2-Diagnose-Endpunkt für die Inbetriebnahme
- mehrere parallele Einsätze und Veranstaltungen als Tabs
- jährliche fortlaufende Einsatznummer
- Status `Ausgerückt` und `Bereitstellung`
- Fahrzeug- und optionale Sitzplatzwahl mit Doppelbelegungsschutz
- Dienste mit fünf Gruppen sowie Veranstaltungen mit einer Teilnehmerliste
- Anwesenheit und durch Führungskräfte gesetzte Entschuldigungen
- Stammdatenpflege für Personen, Qualifikationen, Fahrzeuge, Sitzplätze und Eventtypen
- SQLite für den MVP
- Windows-x64 Self-Contained Deployment und Edge-Kioskstart

## Voraussetzungen für Entwicklung

- .NET 10 SDK
- Windows, Linux oder macOS für die Entwicklung
- für den vorgesehenen Wachenbetrieb: Windows x64 und Microsoft Edge für den Kioskmodus

## Schnellstart

```powershell
dotnet restore .\FireDepartmentMvp.sln
dotnet run --project .\src\FireDepartmentMvp\FireDepartmentMvp.csproj
```

Die tatsächlich verwendeten URLs stehen anschließend in der Konsole (`Now listening on ...`). Der Demo-/Führungs-PIN lautet im Teststand `1234`.

## Projektstruktur

```text
src/FireDepartmentMvp/     Anwendung (Blazor, Domain, EF Core, Services)
docs/                      Architektur, FE2, Deployment, Entwicklerdokumentation
scripts/deploy/             Publish-, Start-, Stop- und Kiosk-Skripte
scripts/test/               Testaufrufe für REST und FE2
FireDepartmentMvp.sln       Visual-Studio-/dotnet-Solution
NuGet.Config                reproduzierbare NuGet-Konfiguration
```

## Dokumentation

- [Entwicklerdokumentation](docs/development.md)
- [Architektur und Fachkonzept](docs/architecture.md)
- [Alamos-FE2-Integration](docs/fe2-integration.md)
- [Deployment auf dem Wachen-PC](docs/station-deployment.md)

## Testalarm

Bei lokal gestarteter Anwendung den Port aus der Konsolenausgabe verwenden. Beispiel:

```powershell
$body = @{
    externalId = "TEST-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    keyword = "FEU"
    message = "Testalarm Gebaeudebrand"
    street = "Musterstrasse"
    houseNumber = "12"
    city = "Musterstadt"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://localhost:60979/api/alarm" `
    -Method Post `
    -ContentType "application/json; charset=utf-8" `
    -Body ([Text.Encoding]::UTF8.GetBytes($body))
```

## Datenschutz

Die Anwendung verarbeitet personenbezogene Anwesenheits- und Einsatzdaten. Vor einem produktiven Betrieb müssen insbesondere Berechtigungskonzept, PIN-/Benutzerverwaltung, Backup, Aufbewahrung/Löschung, Protokollierung und Netzwerkschutz verbindlich festgelegt werden.
