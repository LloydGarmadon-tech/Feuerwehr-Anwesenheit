FEUERWEHR MVP - WACHEN-TEST
===========================

1. Auf dem Entwicklungs-PC im Projektordner ausführen:
   powershell -ExecutionPolicy Bypass -File .\scripts\deploy\publish-windows.ps1

2. Den erzeugten Ordner
   deploy\FireDepartmentWache
   komplett auf den Wachen-PC kopieren, z.B. nach C:\Feuerwehr\FireDepartmentWache

3. Auf dem Wachen-PC start-wache.cmd doppelklicken.
   Die Anwendung läuft unter http://localhost:5080
   Für den Touchmonitor optional start-kiosk-edge.cmd starten.
   Im LAN ist sie über http://<IP-DES-WACHEN-PC>:5080 erreichbar, sofern die Windows-Firewall Port 5080 erlaubt.

4. Gesundheitscheck:
   http://localhost:5080/api/health

5. Stammdaten:
   http://localhost:5080/admin
   Demo-/Führungs-PIN: 1234

6. Testalarm (PowerShell):
   powershell -ExecutionPolicy Bypass -File .\test-alarm-wache.ps1

DATENBANK
---------
Die SQLite-Datei firedepartment.db wird im Programmordner erzeugt.
Vor Updates/Tests diese Datei sichern. Sie enthält die erfassten Daten.

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

Lokaler Test:
  powershell -ExecutionPolicy Bypass -File .\test-fe2-debug.ps1
  powershell -ExecutionPolicy Bypass -File .\test-fe2-alarm.ps1
