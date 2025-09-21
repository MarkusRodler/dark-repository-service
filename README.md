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
{"$type":"DummyCreatedEvent","id":"275c4942-85b2-40f0-a699-a6dc007b1afa","tags":["tag1key:tag1value", "tag2key:tag2value"], "title":"Dummy 1"}
{"$type":"DummyImprovedEvent","id":"275c4942-85b2-40f0-a699-a6dc007b1afa","tags":["tag1key:tag1value", "tag2key:tag2value"], "title":"Dummy One"}
{"$type":"DummyDeletedEvent","id":"275c4942-85b2-40f0-a699-a6dc007b1afa","tags":["tag1key:tag1value", "tag2key:tag2value"]}
```

---

### Daten anhängen
```http
PUT /Append/{aggregate:string}/{id:string}/{version:int}
```

Hängt neue Daten an das Aggregat/ID an.
Body: JSONL (Text, Zeile pro Event)
Optional: `failIf` als Query-Parameter Details siehe: [DCB](#dcb).
**Warnung:** Verändert gleichzeitig das Event indem es ein weiteres Metadaten-Feld `Version` einfügt.
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
Filterung nach Typ, Tags und Datum:
```http
GET /Read/Dummy/0815?query=types=DummyCreatedEvent|DummyDeletedEvent,tags=a1|a2:b1,date=2023-05-06
```
Liefert alle Events vom Typ DummyCreatedEvent oder DummyDeletedEvent, mit Tag a1 oder a2:b1, die am 06.05.2023 erstellt wurden.

ODER-Suche für mehrere Tage:
```http
GET /Read/Dummy/0815?query=date=2023-05-06|2023-05-07
```
Liefert alle Events die am 06.05.2023 oder 07.05.2023 erstellt wurden.

Zeitraum-Suche:
```http
GET /Read/Dummy/0815?query=date=2023-05-06~2023-05-16
```
Liefert alle Events, die zwischen dem 06.05.2023 und 16.05.2023 (inklusive) erstellt wurden.

Kombinierte Suche:
```http
GET /Read/Dummy/0815?query=types=DummyCreatedEvent,date=2023-05-06|2023-05-07~2023-05-16
```
Liefert alle Events vom Typ DummyCreatedEvent, die entweder am 06.05.2023 oder im Zeitraum 07.05.2023 bis 16.05.2023 erstellt wurden.

Die bisherigen Filtermöglichkeiten (Semikolon für Gruppen, | für ODER) funktionieren auch mit dem neuen Feld `date`.
Aus logischen Gründen funktioniert aber nicht + für und.
Es ist auch möglich die Zeit und Zeitzone mit anzugeben. Dabei wird aber immer nur der jeweilige tag berücksichtigt.

## k6
Um Performance-Tests / Last-Tests auszuführen wird [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/#linux) benötigt.
Alle Tests liegen im Ordner `.performance`.
Beispielaufruf: `k6 run .performance/Has.js`
