# FE2-Anbindung über „URL öffnen“

Stand: FE2 2.42.x / Wachen-Teststand

## 1. Was die Anwendung jetzt anbietet

- `GET /api/alarm/fe2/debug` zeigt die empfangenen Query-Parameter an und erzeugt **keinen** Einsatz.
- `GET /api/alarm/fe2` übernimmt den Alarm in die Anwendung.
- `POST /api/alarm` bleibt als bisheriger JSON-Testendpunkt erhalten.

Der produktive GET-Endpunkt versteht folgende Query-Namen:

- `externalId`
- `keyword` (Pflichtfeld)
- `message`
- `street`
- `houseNumber` oder `house`
- `city`
- `alarmTime` oder `timestamp`

## 2. FE2: Plugin „URL öffnen“

Für den ersten Test:

    http://localhost:5080/api/alarm/fe2/debug?

UTF-8 aktivieren.

Wenn FE2 und die Anwendung auf unterschiedlichen Rechnern laufen, `localhost` durch die IP bzw. den Hostnamen des Wachen-PCs ersetzen.

Nach erfolgreichem Debugtest auf produktiv umstellen:

    http://localhost:5080/api/alarm/fe2?

## 3. FE2: Alarmtext [eigene Parameter]

Das Alamos-Plugin „Alarmtext [eigene Parameter]“ kann direkt auf Alarmparameter zugreifen. Eine robuste Konfiguration ist daher, die FE2-Schlüssel selbst den Platzhaltern `&1&` bis `&7&` zuzuweisen.

Empfohlene Zuordnung:

    &1& = externalId
    &2& = keyword
    &3& = message
    &4& = street
    &5& = house
    &6& = city
    &7& = timestamp

Betreff:

    externalId=&1&&keyword=&2&&message=&3&&street=&4&&houseNumber=&5&&city=&6&&alarmTime=&7&

Wichtig: Das Plugin „URL öffnen“ hängt den Betreff an die eingetragene Basis-URL an und kodiert ihn laut Alamos automatisch URL-konform.

Falls einzelne Parameter in eurem konkreten Alarmeingang nicht vorhanden sind, zuerst den Debug-Endpunkt verwenden. Entscheidend ist insbesondere, ob `externalId` bei eurem Leitstellen-Input tatsächlich gesetzt wird.

## 4. Verhalten bei Alarmnachträgen / Duplikaten

- Neue `externalId`: neuer Einsatz, Status `Created`.
- Bereits vorhandene `externalId`, Einsatz noch offen: vorhandener Einsatz wird aktualisiert, Status `Updated`.
- Bereits vorhandene `externalId`, Einsatz geschlossen: bleibt geschlossen, Status `AlreadyClosed`.

Ein geschlossener Einsatz wird durch einen wiederholten FE2-Aufruf niemals erneut geöffnet.

## 5. Lokaler Test ohne FE2

Diagnose:

    powershell -ExecutionPolicy Bypass -File .\test-fe2-debug.ps1

Produktiver Test:

    powershell -ExecutionPolicy Bypass -File .\test-fe2-alarm.ps1

Zum Testen eines Nachtrags dieselbe externe ID zweimal verwenden:

    powershell -ExecutionPolicy Bypass -File .\test-fe2-alarm.ps1 -ExternalId TEST-NACHTRAG-001

Beim ersten Aufruf sollte `Created`, beim zweiten `Updated` zurückgegeben werden.

## 6. Noch nicht Produktion

Der FE2-Endpunkt ist im MVP noch nicht authentifiziert. Er sollte nur in einem vertrauenswürdigen internen Netz erreichbar sein. Für den Produktivbetrieb sollte die Schnittstelle zusätzlich abgesichert werden.
