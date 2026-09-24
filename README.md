# Surface Touch Mediaplayer

[English version](README.en.md)

Surface Touch Mediaplayer ist eine große, touch-freundliche Windows-Oberfläche für den bereits installierten VLC-Videokern. Sie wurde für das Surface Pro 8 bei 2880 × 1920 Pixeln und 200 % Windows-Skalierung gestaltet.

Die Anwendung enthält weder VLC noch Mediencodecs. Sie findet eine vorhandene 64-Bit-Installation von VLC automatisch.

## Hinweis zu Windows SmartScreen

> [!WARNING]
> Die EXE ist **nicht digital signiert**. Windows SmartScreen kann den Download oder den ersten Start mit „Der Computer wurde durch Windows geschützt“ blockieren.
>
> Lade sie nur von den [GitHub-Releases](https://github.com/nylarus90/Surface-Touch-Mediaplayer/releases) herunter und vergleiche ihre SHA-256-Prüfsumme mit der Angabe im jeweiligen Release. Für die direkt heruntergeladene EXE: `Get-FileHash -Algorithm SHA256 .\Surface-Touch-Mediaplayer.exe` in PowerShell ausführen.
>
> Wenn die Prüfsumme stimmt und du der Quelle vertraust, kannst du bei angebotener Option „Weitere Informationen“ und danach „Trotzdem ausführen“ wählen. Auf verwalteten Geräten oder bei aktivem Smart App Control kann diese Option fehlen.

### Fehlerbehebung: EXE startet direkt, aber nicht über eine Verknüpfung

Bei einem von GitHub heruntergeladenen Release wurde dieser konkrete Fall auf einem Surface Pro 8 beobachtet: Die EXE ließ sich direkt starten, der Start derselben Datei über eine `.lnk`-Verknüpfung wurde jedoch blockiert. Ursache war die Internet-Markierung der EXE, der sogenannte **Mark of the Web** (`Zone.Identifier` mit `ZoneId=3`). Nach dem Entfernen dieser Markierung funktionierte auch die Verknüpfung.

1. Prüfe zuerst, dass die SHA-256-Prüfsumme der EXE exakt mit der Angabe im GitHub-Release übereinstimmt:

   ```powershell
   Get-FileHash -Algorithm SHA256 .\Surface-Touch-Mediaplayer.exe
   ```

2. Zeige die gespeicherten NTFS-Datenströme an:

   ```powershell
   Get-Item .\Surface-Touch-Mediaplayer.exe -Stream *
   ```

3. Wenn `Zone.Identifier` vorhanden ist, der Hash stimmt und du die EXE aus diesem Repository geladen hast, entferne nur diese Internet-Markierung:

   ```powershell
   Unblock-File .\Surface-Touch-Mediaplayer.exe
   ```

4. Erstelle die Verknüpfung anschließend lokal neu und teste sie erneut.

`Unblock-File` signiert oder überprüft die Anwendung nicht; es entfernt lediglich die Herkunftsmarkierung. Verwende den Befehl deshalb erst nach der Hash-Prüfung. Smart App Control oder SmartScreen müssen für diesen Workaround nicht deaktiviert werden. Auf verwalteten Geräten können Richtlinien den Start weiterhin verhindern.

## Start

1. Installiere die 64-Bit-Version von [VLC media player](https://www.videolan.org/vlc/).
2. Lade `Surface Touch Mediaplayer.exe` aus den Release-Assets herunter.
3. Starte die EXE mit einem Doppelklick und öffne Medien über `＋` oder ziehe sie in das Fenster.

Die EXE benötigt keine Installation und kann an einem beliebigen Ort gespeichert werden. Sie sucht VLC zuerst im üblichen Installationsordner, danach in den Windows-Registry-Einträgen und zuletzt neben der EXE beziehungsweise im Unterordner `VLC`. Eine 32-Bit-Installation wird nicht unterstützt.

Für eine portable VLC-Installation lege den vollständigen VLC-Ordner als `VLC` neben die EXE. In diesem Unterordner müssen mindestens `libvlc.dll` und der Ordner `plugins` liegen.

## Bedienung

- `▶` und `Ⅱ` starten und pausieren die Wiedergabe, `■` stoppt sie.
- Die großen `◀◀`- und `▶▶`-Tasten wechseln Titel.
- `🔀` schaltet Zufall ein oder aus. Aktiv wird die Taste farbig markiert; alle Titel werden einmal gemischt abgespielt, bevor ein neuer Durchlauf beginnt.
- `🔁` wechselt zwischen aus, gesamte Liste wiederholen und aktuellen Titel wiederholen. Für einen einzelnen Titel wird `🔂` angezeigt.
- `⛶`, `F11` oder `Esc` steuern das Vollbild. `☰` blendet die Wiedergabeliste im normalen Fenster ein oder aus.
- `🔊` und `🔇` steuern den Ton; daneben liegen die Lautstärketasten.
- `⌫` entfernt den markierten Eintrag. `🗑` beendet die Wiedergabe und leert die gesamte Liste.

Die Transporttasten liegen links, die Modus- und Lautstärketasten rechts. Die große eigene Fensterleiste ersetzt die kleinen Windows-Schaltflächen: Ziehen am Titelbereich verschiebt das Fenster, ein Doppelklick maximiert oder stellt es wieder her. Das Fenster lässt sich an den Rändern skalieren; Maximieren lässt die Windows-Taskleiste sichtbar.

Aktive Modi werden farbig markiert. Nach dem Umschalten erscheint kurz eine Statusmeldung. Tooltips, Dialoge, Fehlermeldungen und zugängliche Namen werden anhand der Windows-Anzeigesprache automatisch auf Deutsch oder Englisch dargestellt.

Wiedergabelisten gelten nur für die aktuelle Sitzung und werden beim Schließen verworfen.

## Wiedergabelisten

Unterstützt werden `.xspf`, `.m3u`, `.m3u8`, `.pls`, `.asx`, `.wpl`, `.vlc` und `.ram`. Relative Pfade, lokale Dateien und Stream-Adressen werden in Listenreihenfolge übernommen. Eine HLS-Datei (`.m3u8` mit `#EXT-X-`-Einträgen) wird als Stream an VLC übergeben.

Öffne nur Wiedergabelisten aus vertrauenswürdigen Quellen: Sie können Stream-Adressen enthalten; beim Abspielen kann VLC daher eine Netzwerkverbindung aufbauen. Netzwerk-Dateipfade in Wiedergabelisten werden beim Import nicht übernommen. XML-Listen werden ohne DTD und ohne externe XML-Entitäten eingelesen; Textlisten sind auf 10 MB und 10.000 Einträge begrenzt.

## Datenschutz und Protokoll

Die Anwendung sammelt keine Telemetrie und enthält keine Update-Funktion.

Eine Laufzeitdatei `TouchPlayer.log` kann neben der EXE entstehen. Sie enthält technische Start- und Beendigungsinformationen und gehört nicht in ein Issue oder Release-Archiv.

## Entwicklung

Hinweise zum lokalen Build, zu Tests und zum Erstellen eines Release-Archivs stehen in [BUILDING.md](BUILDING.md). Die Änderungen der aktuellen Version stehen in [CHANGELOG.md](CHANGELOG.md). Vor dem Veröffentlichen hilft die [Release-Checkliste](RELEASE_CHECKLIST.md).

## Abhängigkeit und Marken

VLC wird nicht mitgeliefert. VLC ist ein Projekt und eine Marke von VideoLAN. Die Lizenzbedingungen von VLC stehen im [VLC-Quellcode](https://code.videolan.org/videolan/vlc/-/blob/master/COPYING). Dieses Projekt ruft die lokal installierte VLC-Bibliothek dynamisch auf und enthält keine VLC-Binärdateien.

Surface Touch Mediaplayer steht unter der [MIT-Lizenz](LICENSE). Sie gilt für den eigenen Quellcode dieses Projekts, nicht für VLC oder dessen Bestandteile.
