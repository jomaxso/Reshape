# Reshape CLI - Code-Struktur

## Überblick

Der Code wurde refaktoriert, um die **Separation of Concerns** zu verbessern und die Wartbarkeit zu erhöhen. Jeder Command Handler ist nun in einer eigenen Datei organisiert.

## Projektstruktur

```
reshape-cli/
├── Commands/                          # Command Handler (ein Handler pro Datei)
│   ├── ServeCommandHandler.cs        # 'serve' - Startet die Web UI
│   ├── ListCommandHandler.cs         # 'list' - Zeigt Dateien an
│   ├── PreviewCommandHandler.cs      # 'preview' - Vorschau der Umbenennungen
│   ├── RenameCommandHandler.cs       # 'rename' - Führt Umbenennungen aus
│   └── PatternsCommandHandler.cs     # 'patterns' - Zeigt verfügbare Muster
│
├── Utilities/                         # Helper-Klassen
│   └── FormatHelper.cs               # Formatierungs-Hilfsfunktionen
│
├── Program.cs                         # Einstiegspunkt und Command-Konfiguration
├── FileService.cs                     # Datei-Operationen und Metadaten
├── Models.cs                          # Datenmodelle (Records)
└── AppJsonSerializerContext.cs       # JSON-Serialisierung
```

## Vorteile der neuen Struktur

### 1. **Single Responsibility Principle**
- Jeder Command Handler hat eine klare, einzelne Verantwortung
- Einfacher zu testen und zu warten

### 2. **Bessere Lesbarkeit**
- `Program.cs` ist jetzt auf ~80 Zeilen reduziert (vorher ~350 Zeilen)
- Die Command-Konfiguration ist auf einen Blick erfassbar
- Implementierungsdetails sind in dedizierte Dateien ausgelagert

### 3. **Einfachere Wartung**
- Änderungen an einem Command betreffen nur eine Datei
- Keine riesigen Dateien mehr durchsuchen
- Klare Namenskonventionen

### 4. **Erweiterbarkeit**
- Neue Commands können einfach hinzugefügt werden
- Copy-Paste-Vorlage bereits vorhanden
- Konsistente Struktur für alle Handler

## Command Handler Pattern

Alle Command Handler folgen einem konsistenten Muster:

```csharp
namespace Reshape.Cli.Commands;

/// <summary>
/// Handles the 'command-name' command.
/// </summary>
internal static class CommandNameHandler
{
    public static int Execute(/* parameter */)
    {
        try
        {
            // Command-Logik hier
            return 0; // Erfolg
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1; // Fehler
        }
    }
    
    // Private Helper-Methoden zur Organisation der Logik
    private static void HelperMethod() { }
}
```

## Nächste Schritte (optional)

Mögliche weitere Verbesserungen:

1. **Unit Tests** für jeden Command Handler hinzufügen
2. **Dependency Injection** für FileService einführen
3. **Logging** mit ILogger statt Console-Ausgaben
4. **Validator-Klassen** für Input-Validierung
5. **Configuration** in appsettings.json auslagern
