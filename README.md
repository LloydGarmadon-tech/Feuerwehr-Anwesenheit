# Feuerwehr-Anwesenheit

Touch-optimierte Webanwendung für Feuerwehrwachen zur Übernahme von Alarmen aus **Alamos FE2**, Erfassung der Einsatz- und Dienstanwesenheit sowie Verwaltung von Fahrzeugen, Sitzplätzen und Qualifikationen.

## Funktionsumfang

- Alarme aus Alamos FE2 übernehmen
- mehrere parallele Einsätze als Tabs anzeigen
- jährliche Einsatzzahl erfassen
- Einsatzteilnahme je Mitglied dokumentieren
  - ausgerückt
  - in Bereitschaft
  - Fahrzeug
  - optionaler Sitzplatz
- Sitzplätze abhängig vom gewählten Fahrzeug und der aktuellen Belegung anbieten
- offene Einträge bis zum Abschluss korrigieren
- abgeschlossene Einsätze aus der Touch-Oberfläche ausblenden
- Anwesenheitslisten für Dienste und Veranstaltungen
- fünf Gruppen bei Gruppen-, Sonder-, Wehr- und Zugdiensten
- Einzelansicht für Übungen, Veranstaltungen, Maschinisten, Führungsunterstützung und Atemschutzdienste
- Entschuldigungen durch Führungskräfte
- Stammdatenpflege für Personen, Gruppen, Qualifikationen, Fahrzeuge und Sitzplätze
- Archivbasis für spätere Statistiken
- Windows-/Kiosk-Deployment

## Projektstruktur

```text
src/FireDepartmentMvp/     Anwendung
docs/                      Entwickler- und Betriebsdokumentation
scripts/deploy/             Windows-Publish und Wachenstart
scripts/test/               Testaufrufe für API und FE2
```

Weitere Dokumentation:

- [Entwicklerdokumentation](docs/development.md)
- [Architektur](docs/architecture.md)
- [Alamos-FE2-Integration](docs/fe2-integration.md)
- [Deployment auf dem Wachen-PC](docs/station-deployment.md)

## Lokal starten

Voraussetzung ist das zum Projekt passende .NET SDK.

```powershell
dotnet restore
dotnet run --project .\src\FireDepartmentMvp\FireDepartmentMvp.csproj
```

Die von ASP.NET Core ausgegebenen lokalen URLs verwenden. Der Port kann beim lokalen Entwicklungsstart von der späteren Wachen-Konfiguration abweichen.

## Alarm-API

Standard-JSON-Schnittstelle:

```text
POST /api/alarm
```

FE2-Diagnose:

```text
GET /api/alarm/fe2/debug
```

FE2-Alarmübernahme:

```text
GET /api/alarm/fe2
```

Health Check:

```text
GET /api/health
```

Details zur Alamos-Konfiguration stehen in `docs/fe2-integration.md`.

## Datenhaltung

Der MVP verwendet SQLite. Laufzeitdatenbanken gehören nicht ins Repository und werden über `.gitignore` ausgeschlossen.

## Status

Das Projekt befindet sich im MVP-/Teststadium. Vor einem dauerhaften Produktivbetrieb sollten insbesondere Authentifizierung/PIN-Konzept, Datenschutz, Backup, Protokollierung, Datenbankmigrationen und automatisierte Tests weiter ausgebaut werden.
