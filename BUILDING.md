# Bauen und prüfen

## Voraussetzungen

- Windows 10 oder Windows 11 (64 Bit)
- .NET Framework 4.8 Developer Pack mit dem 64-Bit-C#-Compiler
- Für den manuellen Player-Test: eine 64-Bit-VLC-Installation; der Player erkennt den Standardpfad, die Registry und eine portable Installation im Unterordner `VLC` neben der EXE.

Die Tests importieren Wiedergabelisten und prüfen die Oberfläche. Sie benötigen keine Medien und starten keine Wiedergabe.

## Build

In PowerShell im Projektordner:

```powershell
.\build.ps1 -Test
```

Die fertige Anwendung liegt danach unter `dist\Surface Touch Mediaplayer.exe`. Der Build ist explizit 64-bittig, passend zur installierten 64-Bit-VLC-Bibliothek.

## Release-Archiv erstellen

```powershell
.\build.ps1 -Test -Package -Version 1.0.1
```

Die EXE unter `dist\Surface Touch Mediaplayer.exe` und das Archiv `dist\Surface-Touch-Mediaplayer-1.0.1-win-x64.zip` sind für die Assets eines GitHub-Releases vorgesehen. VLC-Bibliotheken und VLC-Plugins werden bewusst nicht eingepackt.
