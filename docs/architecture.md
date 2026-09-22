# Fach- und Softwaredesign

## 1. Zielbild

Die Anwendung läuft zentral auf einem Windows-Server und wird auf dem Touchdisplay im Browser/Kioskmodus dargestellt. Sie verwaltet parallel laufende Einsätze und manuell gestartete Events. Die öffentliche Touchansicht zeigt ausschließlich offene Vorgänge. Geschlossene Vorgänge sind dort nicht mehr abrufbar und werden später nur im geschützten Verwaltungsbereich angezeigt.

## 2. Vorgangsarten

### Einsatz

Wird über eine externe Schnittstelle angelegt (später Alamos-FE2-Adapter). Pro Einsatz werden Alarmdaten, Mannschaft, Status, Fahrzeug und optional Sitzplatz gespeichert.

Teilnahmestatus:
- Ausgerückt
- Bereitstellung

### Event

Wird nach PIN-Freischaltung manuell gestartet. Eventtypen sind Stammdaten und bestimmen das Layout.

5-Gruppen-Layout:
- Gruppendienst
- Sonderdienst
- Wehrdienst
- Zugdienst

Einzelliste:
- Übung
- Veranstaltung
- Maschinistendienst
- Führungsunterstützung
- Atemschutzdienst

Eventtypen sollen administrativ erweiterbar sein.

## 3. Bedienfluss

### Event starten

1. `Führung` auswählen.
2. PIN eingeben.
3. `Event starten` auswählen.
4. Eventtyp auswählen.
5. Titel eingeben.
6. Event öffnen.
7. Bei 5-Gruppen-Layout erscheinen Gruppe 1 bis 5 als Untertabs; bei Einzelliste erscheint eine gemeinsame Teilnehmerliste.

### Eventteilnahme

Ein Kamerad wählt seinen Namen und setzt sich auf `Anwesend`. Solange das Event offen ist, kann der Eintrag zurückgenommen und erneut gesetzt werden.

`Entschuldigt` ist eine Führungsfunktion. In der Produktivversion wird der angemeldete Gruppenführer und die betroffene Gruppe geprüft und als Ändernder protokolliert.

### Einsatzteilnahme

1. Kamerad auswählen.
2. `Ausgerückt` oder `Bereitstellung` wählen.
3. Bei `Ausgerückt` Fahrzeug auswählen.
4. Optional freien Sitzplatz des gewählten Fahrzeugs auswählen.
5. Teilnahme speichern.

Der Eintrag kann bis zum Einsatzabschluss korrigiert oder gelöscht werden.

### Sitzplätze

- Sitzplätze sind fahrzeugbezogene Stammdaten.
- Sitzplatz ist kein Pflichtfeld.
- Es werden nur Plätze des ausgewählten Fahrzeugs angeboten.
- Bereits im selben Einsatz belegte Plätze sind nicht auswählbar.
- Serverseitig verhindert ein Unique-Constraint doppelte Platzbelegung.
- Fahrzeugwechsel gibt den alten Platz wieder frei.

### Abschluss

Eine Führungskraft schließt den Vorgang. Danach:

1. Status `Closed` wird gespeichert.
2. Vorgang verschwindet sofort aus allen öffentlichen Touchansichten.
3. Normale Änderungen werden serverseitig abgewiesen.
4. Produktiv: PDF erzeugen.
5. Produktiv: Druckauftrag auslösen.
6. Produktiv: Datensatz im geschützten Archiv verfügbar machen.

Ein Druckfehler darf den fachlichen Abschluss niemals rückgängig machen.

## 4. Mehrere parallele Vorgänge

Es gibt keinen globalen `CurrentMission`. Die Anwendung arbeitet mit einer Liste `ActiveActivities`.

Beispiel:

- Einsatz 101/2026
- Einsatz 102/2026
- Wehrdienst "Fahrzeugkunde"
- Atemschutzdienst "Belastungsübung"

Alle Vorgänge sind voneinander unabhängig. Ein neu eingehender Alarm darf eine laufende Eingabe nicht automatisch auf einen anderen Tab umschalten.

## 5. Stammdaten

### Person

- Vorname
- Nachname
- Aktiv
- Gruppe
- Qualifikationen

Später optional:
- Personalnummer
- Dienstgrad
- Eintritt/Austritt
- weitere organisatorische Angaben

### Gruppe

- Nummer
- Name
- Aktiv

Aktuell fünf Gruppen.

### Qualifikation

n:m-Beziehung Person ↔ Qualifikation.

Beispiele:
- Atemschutzgeräteträger (AGT)
- Maschinist
- Gruppenführer
- Zugführer
- Führungsunterstützung

Optional `ValidUntil`, damit zeitlich begrenzte Qualifikationen später ausgewertet werden können.

Qualifikationen schränken die Teilnahme an Diensten grundsätzlich nicht ein. Sie dienen zunächst der Information und später der Einsatz-/Ausbildungsstatistik.

### Fahrzeug

- Name
- Funkrufname
- Aktiv
- Sitzplätze

### Sitzplatz

- Fahrzeug
- Name/Funktion
- Sortierreihenfolge
- Aktiv

### Eventtyp

- Name
- Layout (`SingleList` oder `FiveGroups`)
- Sortierung
- Aktiv

## 6. Berechtigungsmodell Produktion

Benutzerkonto und Feuerwehrperson werden getrennt modelliert.

Rollen:
- Gruppenführer
- Wehr-/Zugführung
- Administrator

PIN wird nicht im Klartext gespeichert, sondern gehasht.

Geplante Regeln:
- normale Teilnahme: kein Login erforderlich
- Event starten/schließen: Führungskraft
- Entschuldigung: Gruppenführer für seine Gruppe
- Stammdaten/Archiv/Statistik: geschützter Verwaltungsbereich
- Führungsmodus wird nach konfigurierbarer Inaktivität automatisch gesperrt

Der Prototyp verwendet für einfaches Testen einen gemeinsamen Demo-PIN `1234`.

## 7. Datenmodell

```text
Activity
  Id
  Kind                Mission | Event
  Status              Open | Closed
  Title
  StartedAt
  ClosedAt
  EventTypeId?

MissionDetails
  ActivityId
  ExternalId
  Year
  Number
  Keyword
  AlarmMessage
  Street
  HouseNumber
  City
  AlarmedAt

EventType
  Id
  Name
  Layout               SingleList | FiveGroups
  SortOrder
  IsActive

Attendance
  Id
  ActivityId
  FirefighterId
  Status
  VehicleId?
  VehicleSeatId?
  RegisteredAt
  UpdatedAt?
  ChangedBy?

Firefighter
  Id
  FirstName
  LastName
  GroupId
  IsActive

FirefighterGroup
  Id
  Number
  Name
  IsActive

Qualification
  Id
  Name
  ShortName
  IsActive

FirefighterQualification
  FirefighterId
  QualificationId
  ValidUntil?

Vehicle
  Id
  Name
  CallSign
  IsActive

VehicleSeat
  Id
  VehicleId
  Name
  SortOrder
  IsActive
```

Wichtige Constraints:

- `(ActivityId, FirefighterId)` eindeutig
- `(ActivityId, VehicleSeatId)` eindeutig, wenn Sitzplatz gesetzt
- externe Alamos-ID soll eindeutig/idempotent verarbeitet werden
- Einsatznummer soll in Produktion transaktional pro Jahr vergeben werden

## 8. Schnittstelle für Alarme

Prototyp:

```http
POST /api/alarm
Content-Type: application/json
```

Beispiel:

```json
{
  "externalId": "ALAMOS-12345",
  "alarmTime": "2026-09-08T11:30:00+02:00",
  "keyword": "FEU",
  "message": "Brennt Gartenlaube",
  "street": "Musterstraße",
  "houseNumber": "17",
  "city": "Musterstadt"
}
```

Die konkrete FE2-Anbindung wird als Adapter vor diesen stabilen Anwendungsendpunkt gesetzt.

## 9. Zielarchitektur Produktion

```text
Alamos FE2
    |
    v
Alarm Adapter / REST Endpoint
    |
    v
ASP.NET Core / Blazor Web App
    |
    +-- ActivityService
    +-- AttendanceService
    +-- Authorization/PIN
    +-- PDF/Print Service
    +-- Statistics
    |
    v
EF Core
    |
    v
SQL Server
```

Kein Microservice-System erforderlich. Ein modularer Monolith ist für den Gerätehausbetrieb einfacher zu installieren, zu sichern und zu warten.

## 10. Produktionsausbau nach dem Touch-MVP

1. SQL Server + EF-Core-Migrations
2. Adminbereich für Personen, Gruppen, Fahrzeuge, Sitzplätze, Qualifikationen und Eventtypen
3. Benutzer-/PIN-/Rollenmodell mit Gruppenberechtigung
4. Archiv
5. PDF-Protokoll
6. Windows-Druckservice
7. Statistik (Einsätze, Teilnahmen, Fahrzeuge, Sitzplätze, Qualifikationen)
8. Alamos-FE2-Adapter
9. Backupstrategie
10. Audit/Änderungshistorie
11. automatisierte Unit-/Integrationstests
12. Kiosk-/Windows-Server-Deployment
