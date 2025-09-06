# Repository (Service)
![Service](https://github.com/MarkusRodler/dark-repository-service/workflows/Service/badge.svg)

## API-Dokumentation


### Health Check
```http
GET /heartbeat
```
Prüft, ob der Service läuft.

---

### IDs für Aggregate abrufen
```http
GET /GetIdsFor/{aggregate:string}
```
Gibt alle IDs für das angegebene Aggregat zurück.

Antwort:
```json
["id1", "id2", ...]
```

---

### Existenz prüfen
```http
GET /Has/{aggregatestring}/{id:string}
```
Prüft, ob ein Eintrag mit der ID existiert.

Http Code     | Bedeutung
------------- | ---------------
200 OK        | Existiert
404 Not Found | Existiert nicht

---

### Daten lesen
```http
GET /Read/{aggregate:string}/{id:string}
GET /Read/{aggregate:string}/{id:string}.jsonl
GET /Read/{aggregate:string}/{id:string}/{afterLine:int?}
```
Liest die Daten als [JSONL](https://jsonlines.org/) für das Aggregat und die ID.
Der zweite Aufruf liefert direkt statische Dateien aus dem `Data`-Verzeichnis.
Liefert die Daten als Stream optional ab einer bestimmten Zeile. Es kann ein Query-String für Filterung übergeben werden. Details siehe: [DCB](#dcb)

Antwort:
`Content-Type: application/jsonl; charset=utf-8`
```jsonl
{"$type":"DummyCreatedEvent","id":"275c4942-85b2-40f0-a699-a6dc007b1afa",tags:["tag1key:tag1value", "tag2key:tag2value"], "title":"Dummy 1"}
{"$type":"DummyImprovedEvent","id":"275c4942-85b2-40f0-a699-a6dc007b1afa",tags:["tag1key:tag1value", "tag2key:tag2value"], "title":"Dummy One"}
{"$type":"DummyDeletedEvent","id":"275c4942-85b2-40f0-a699-a6dc007b1afa",tags:["tag1key:tag1value", "tag2key:tag2value"]}
```

---

### Daten anhängen
```http
PUT /Append/{aggregate:string}/{id:string}/{version:int}
```

Hängt neue Daten an das Aggregat/ID an.
Body: JSONL (Text, Zeile pro Event)
Optional: `failIf` als Query-Parameter Details siehe: [DCB](#dcb).
**Warnung:** Verändert gleichzeitig das Event indem es ein weiteres Metadaten-Feld `$ver` einfügt.
Dieses gibt an welche Version es ist.
Die Version deckt sich mit der jeweiligen Zeilennummer.
Von daher ist es unerlässlich, dass die Anzahl der Zeilen niemals verändert werden.
=> Keine Events löschen. Falls dies doch mal benötigt wird lieber das Event modifizieren mit Fake-Infos oder die Zeile komplett leer lassen.

---

### Daten überschreiben
```http
POST /Overwrite/{aggregate:string}/{id:string}/{version:int}
```

Überschreibt komplette Daten für das angegebene Aggregat/ID.
Body: JSONL (Text, Zeile pro Event)
Optional: `failIf` als Query-Parameter Details siehe: [DCB](#dcb).

---

**Hinweis:**
Alle Endpunkte erwarten und liefern UTF-8-kodierte Daten.
Events werden als JSONL (eine JSON pro Zeile) verarbeitet.

## DCB
Dieser Service unterstützt Dynamic Consistency Boundary. Siehe auch: https://dcb.events/
Zur Filterung steht bspw. `query` bei `Read` zur Verfügung und `failIf` bei `Append` und `Overwrite`.

### Beispiel
Filterung nach
```http
GET /Read/Dummy/0815?query=types=DummyCreatedEvent|DummyDeletedEvent,tags=a1|a2:b1
```
liefert nur Events zurück die entweder vom $type DummyCreatedEvent oder DummyDeletedEvent sind und gleichzeitig einen Tag a1 mit beliebigen oder keinen Value hat und einem Tag a2 mit einem Value b1.
Es werden mehrere Gruppen unterstüzt. Diese sind durch ein Semikolon getrennt. Dadurch wird die Ergebnismenge unter Umständen vergrößert.
Beispiel:
```http
GET /Read/Dummy/0815?query=types=DummyCreatedEvent|DummyDeletedEvent,tags=a1|a2:b1;types=DummyImprovedEvent
```
Dadurch werden zusätzlich zu der vorherigen Filterung auch alle Events vom Typ `DummyImprovedEvent` mit aufgenommen.

Falls nur Events gelesen werden sollen die nach der Zeile 33 dazugekommen sind mit allen Filterungen muss diese nur nach dem Pfad mit angegeben werden.
```http
GET /Read/Dummy/0815/33?query=types=DummyCreatedEvent|DummyDeletedEvent,tags=a1|a2:b1;types=DummyImprovedEvent
```
Bei `/Append` und `/Overwrite` kann eine Condition hinzugefügt werden die verhindert, dass Daten geschrieben werden wenn sich diese während der Übertragung geändert haben.

Falls das DCB-Feature nicht genutzt wird fungiert die `version` als harte Angabe wie viele Zeilen das Aggregate vor Abänderung haben soll. Jede Zeile ist dabei eine Version beginnend bei Zeile 1.

Anstatt von **|** was einem **Oder** entspricht ist es auch möglich ein **+** zu verwenden was einem **Und** entspricht.

## k6
Um Performance-Tests / Last-Tests auszuführen wird [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/#linux) benötigt.
Alle Tests liegen im Ordner `.performance`.
Beispielaufruf: `k6 run .performance/Has.js`
