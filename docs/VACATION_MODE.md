# Vacation Mode - Urlaubsfotos organisieren

## Übersicht
Der **Vacation Mode** (Urlaubsmodus) ist eine erweiterte Funktion in Reshape, die speziell für das Organisieren von Urlaubsfotos entwickelt wurde.

## Features

### 🗓️ Tages-basierte Ordnerstruktur
- Automatische Gruppierung von Fotos nach Tagen
- Ordner werden benannt wie "Tag 1", "Tag 2", "Day 1", etc.
- Zählung beginnt ab dem ersten gefundenen Foto oder einem benutzerdefinierten Startdatum

### 🌍 GPS & Zeitzone-Erkennung
- Extrahiert GPS-Koordinaten aus EXIF-Daten
- Erkennt automatisch die Zeitzone basierend auf GPS-Position
- Konvertiert alle Zeitstempel zu UTC für korrekten Tages-Vergleich
- Fotos von verschiedenen Kameras werden korrekt synchronisiert

### 📁 Flexible Ordnerstruktur
- **Tag-Ordner**: Hauptordner für jeden Tag
- **Unterordner (optional)**: Zusätzliche Gruppierung innerhalb der Tages-Ordner
  - z.B. nach Kamera-Modell
  - z.B. nach Tageszeit
  - Oder beliebige andere Metadaten

### 🎯 Intelligente Dateiauswahl
- Nur Fotos mit EXIF-Datum werden verarbeitet
- Fotos ohne Datum bleiben unverändert (werden deselektiert)
- Warnung bei fehlenden GPS-Daten (Fallback auf lokale Zeit)

## Verwendung

### Web UI

1. **Ordner scannen**: Wählen Sie einen Ordner mit Urlaubsfotos
2. **Vacation Mode aktivieren**: Klicken Sie auf den Toggle im lila Panel
3. **Startdatum festlegen** (optional):
   - Leer lassen = automatisch vom ersten Foto
   - Oder manuell ein Datum wählen
4. **Tag-Ordner Pattern**: z.B. `Tag {day_number}` oder `Day {day_number}`
5. **Datei-Pattern**: z.B. `{year}-{month}-{day}_{counter:3}`
6. **Unterordner** (optional): z.B. `{camera_model}` oder `{time_taken}`
7. **Preview**: Prüfen Sie die Vorschau
8. **Execute**: Führen Sie die Umbenennung aus

### Verfügbare Platzhalter

#### Vacation-Mode spezifisch:
- `{day_number}` - Tag-Nummer im Urlaub (1, 2, 3, ...)
- `{day_counter}` - Counter innerhalb eines Tages (001, 002, ...)
- `{global_counter}` - Durchgehender Counter über alle Tage (0001, 0002, ...)

#### Standard-Platzhalter:
- `{year}`, `{month}`, `{day}` - Datum-Komponenten
- `{date_taken}` - Vollständiges Datum (YYYY-MM-DD)
- `{time_taken}` - Uhrzeit (HH-MM-SS)
- `{filename}` - Original Dateiname
- `{camera_make}` - Kamera-Hersteller
- `{camera_model}` - Kamera-Modell
- `{counter:N}` - Zähler mit N-stelliger Padding

## Beispiele

### Beispiel 1: Einfache Tages-Ordner
```
Einstellungen:
- Tag-Ordner: "Tag {day_number}"
- Datei-Pattern: "{time_taken}_{filename}"

Ergebnis:
Tag 1/
  ├─ 09-30-15_IMG_001.jpg
  ├─ 10-45-20_IMG_002.jpg
Tag 2/
  ├─ 08-15-00_IMG_003.jpg
  └─ 14-20-30_IMG_004.jpg
```

### Beispiel 2: Mit Kamera-Unterordnern
```
Einstellungen:
- Tag-Ordner: "Day {day_number}"
- Unterordner: "{camera_model}"
- Datei-Pattern: "{counter:4}"

Ergebnis:
Day 1/
  ├─ iPhone15/
  │   ├─ 0001.jpg
  │   └─ 0002.jpg
  ├─ Canon_EOS_R5/
  │   ├─ 0001.jpg
  │   └─ 0002.jpg
Day 2/
  ├─ iPhone15/
  │   └─ 0001.jpg
  └─ Canon_EOS_R5/
      └─ 0001.jpg
```

### Beispiel 3: Datum + Tag-Counter
```
Einstellungen:
- Tag-Ordner: "Tag {day_number} - {date_taken}"
- Datei-Pattern: "{year}{month}{day}_{day_counter}"

Ergebnis:
Tag 1 - 2026-01-10/
  ├─ 20260110_001.jpg
  ├─ 20260110_002.jpg
Tag 2 - 2026-01-11/
  ├─ 20260111_001.jpg
  └─ 20260111_002.jpg
```

## Technische Details

### Zeitzone-Konvertierung
Die Zeitzone wird basierend auf den GPS-Koordinaten geschätzt:
- Längengrad wird in Stunden-Offset umgerechnet (15° = 1 Stunde)
- Mapping zu bekannten Zeitzonen (z.B. Europe/Paris, Asia/Tokyo)
- UTC-Konvertierung für präzisen Tages-Vergleich
- **Wichtig**: Dies ist eine vereinfachte Schätzung. Für 100% Genauigkeit sollte ein Timezone-Lookup-Service verwendet werden.

### Fallback-Verhalten
- **Kein GPS**: Lokale Zeit wird verwendet (kann zu Ungenauigkeiten führen)
- **Kein EXIF-Datum**: Datei wird nicht verschoben
- **Keine Fotos mit Datum gefunden**: Fallback auf Standard-Rename-Modus

### Performance
- Alle Metadaten werden beim Scan extrahiert
- Preview-Generierung ist in-memory
- Ordner werden on-demand erstellt beim Execute

## Hinweise

⚠️ **Wichtig**:
- Erstellen Sie vor der ersten Verwendung ein Backup Ihrer Fotos
- Testen Sie zuerst mit wenigen Dateien
- Die Preview zeigt die komplette Ordnerstruktur

💡 **Tipps**:
- GPS-Daten sind in Smartphones meist verfügbar
- Professionelle Kameras benötigen oft GPS-Zubehör
- Bei fehlenden GPS-Daten: Startdatum manuell setzen
- Counter im Datei-Pattern vermeiden, wenn Unterordner verwendet werden

## API-Verwendung

```typescript
const vacationMode: VacationModeOptions = {
    enabled: true,
    dayFolderPattern: "Tag {day_number}",
    startDate: "2026-01-10",  // Optional
    subfolderPattern: "{camera_model}"  // Optional
};

const response = await api.previewRename(
    folderPath,
    "{year}-{month}-{day}_{day_counter}",
    ['.jpg', '.png'],
    vacationMode
);
```

## Zukünftige Erweiterungen

Geplante Features:
- [ ] Bessere Timezone-Erkennung (GeoTimeZone Library)
- [ ] Video-Unterstützung (EXIF aus MP4/MOV)
- [ ] Batch-Verarbeitung mehrerer Urlaube
- [ ] Export der Ordnerstruktur als Vorlage
- [ ] Automatische Erkennung von Urlaubs-Zeiträumen
