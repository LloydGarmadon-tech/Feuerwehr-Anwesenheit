FEUERWEHR MVP - WACHEN-TEST
===========================

1. Auf dem Entwicklungs-PC im Projekt-Hauptordner ausführen:

   powershell.exe -ExecutionPolicy Bypass -File .\scripts\deploy\publish-windows.ps1

   Das Publish-Skript erzeugt ein Windows-x64 Self-Contained Deployment und kopiert die benötigten Start-, Test- und Dokumentationsdateien in den fertigen Deployment-Ordner.

2. Der fertige Stand befindet sich anschließend unter:

   deploy\FireDepartmentWache

   Diesen kompletten Ordner auf den Wachen-PC kopieren, z.B. nach:

   C:\Feuerwehr\FireDepartmentWache

3. Auf dem Wachen-PC ausschließlich die start-wache.cmd aus dem fertigen Deployment-Ordner starten:

   C:\Feuerwehr\FireDepartmentWache\start-wache.cmd

   Die Anwendung läuft unter:

   http://localhost:5080

   Für den Touchmonitor kann anschließend optional start-kiosk-edge.cmd gestartet werden.
   Im LAN ist die Anwendung über http://<IP-DES-WACHEN-PC>:5080 erreichbar, sofern die Windows-Firewall Port 5080 erlaubt.

WICHTIG ZUM STARTSKRIPT
-----------------------
Die Datei

   scripts\deploy\start-wache.cmd

ist die Quelldatei für den Deployment-Prozess. Sie darf nicht direkt aus scripts\deploy gestartet werden.

Die start-wache.cmd verwendet den Ordner, in dem sie selbst liegt, um FireDepartmentMvp.exe zu finden. Das Publish-Skript kopiert sie deshalb zusammen mit der Anwendung nach:

   deploy\FireDepartmentWache

Gestartet wird ausschließlich die dort befindliche Kopie. Wird start-wache.cmd direkt aus scripts\deploy ausgeführt, sucht sie FireDepartmentMvp.exe fälschlicherweise in scripts\deploy und der Start schlägt fehl.

Der korrekte lokale Ablauf lautet daher:

   powershell.exe -ExecutionPolicy Bypass -File .\scripts\deploy\publish-windows.ps1
   cd .\deploy\FireDepartmentWache
   .\start-wache.cmd

4. Gesundheitscheck:

   http://localhost:5080/api/health

   Erwartete Antwort: OK

5. Stammdaten:

   http://localhost:5080/admin

   Demo-/Führungs-PIN: 1234

6. Testalarm (PowerShell):

   powershell.exe -ExecutionPolicy Bypass -File .\test-alarm-wache.ps1

DATENBANK
---------
Die SQLite-Datei firedepartment.db wird im Programmordner erzeugt.
Vor Updates/Tests diese Datei sichern. Sie enthält die erfassten Daten.

Bei einem neuen Deployment darf eine vorhandene produktive Datenbank auf dem Wachen-PC nicht versehentlich überschrieben oder gelöscht werden.

WICHTIG
-------
Dies ist ein Teststand, noch kein produktionsreifes Einsatzsystem.
Insbesondere Authentifizierung/PIN, Backups, Archiv/PDF/Druck und die konkrete FE2-Anbindung werden für Produktion weiter gehärtet.

============================================================
FE2-ANBINDUNG (URL ÖFFNEN)
============================================================

Die Details stehen in fe2-integration.md.

Debug-URL:
  http://localhost:5080/api/alarm/fe2/debug?

Produktiv-URL:
  http://localhost:5080/api/alarm/fe2?

UTF-8 im FE2-Plugin "URL öffnen" aktivieren.

Vor der Anbindung an das FE2-Echtsystem zuerst den Debug-Endpunkt verwenden. Dadurch kann geprüft werden, welche Parameter FE2 tatsächlich übermittelt, ohne direkt einen Einsatz anzulegen.

Lokaler Test:
  powershell.exe -ExecutionPolicy Bypass -File .\test-fe2-debug.ps1
  powershell.exe -ExecutionPolicy Bypass -File .\test-fe2-alarm.ps1
