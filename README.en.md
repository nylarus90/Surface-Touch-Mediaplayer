# Surface Touch Mediaplayer

[Deutsche Version](README.md)

Surface Touch Mediaplayer provides a large, touch-friendly Windows interface for an installed VLC playback engine. It is designed for the Surface Pro 8 at 2880 × 1920 pixels with 200% Windows display scaling.

The application includes neither VLC nor media codecs. It automatically locates an existing 64-bit VLC installation.

## Windows SmartScreen notice

> [!WARNING]
> The EXE is **not digitally signed**. Windows SmartScreen may block the download or first launch with a “Windows protected your PC” warning.
>
> Download only from the [GitHub releases](https://github.com/nylarus90/Surface-Touch-Mediaplayer/releases) and compare the file's SHA-256 checksum with the value in that release. For the directly downloaded EXE, run `Get-FileHash -Algorithm SHA256 .\Surface-Touch-Mediaplayer.exe` in PowerShell.
>
> If the checksum matches and you trust the source, choose “More info” and then “Run anyway” when Windows offers those options. Managed devices or Smart App Control may not offer an override.

### Troubleshooting: the EXE starts directly but its shortcut does not

This specific case was observed with a GitHub release on a Surface Pro 8: the EXE started directly, but Windows blocked the same file when it was launched through a `.lnk` shortcut. The cause was the EXE's Internet origin marker, known as **Mark of the Web** (`Zone.Identifier` with `ZoneId=3`). The shortcut worked after that marker was removed.

1. First verify that the EXE's SHA-256 checksum exactly matches the value in the GitHub release:

   ```powershell
   Get-FileHash -Algorithm SHA256 .\Surface-Touch-Mediaplayer.exe
   ```

2. Display the stored NTFS data streams:

   ```powershell
   Get-Item .\Surface-Touch-Mediaplayer.exe -Stream *
   ```

3. If `Zone.Identifier` is present, the checksum matches, and you downloaded the EXE from this repository, remove only that Internet origin marker:

   ```powershell
   Unblock-File .\Surface-Touch-Mediaplayer.exe
   ```

4. Create the shortcut locally again and retest it.

`Unblock-File` does not sign or validate the application; it only removes the origin marker. Use it only after checking the hash. Smart App Control or SmartScreen do not need to be disabled for this workaround. Policies on managed devices may still prevent the application from starting.

## Getting started

1. Install the 64-bit version of [VLC media player](https://www.videolan.org/vlc/).
2. Download `Surface Touch Mediaplayer.exe` from the release assets.
3. Double-click the EXE, then open media with `＋` or drag files into the window.

The EXE needs no installation and can be stored anywhere. It searches first in the standard VLC installation directory, then in Windows Registry entries, and finally beside the EXE or in a `VLC` subdirectory. A 32-bit VLC installation is not supported.

For a portable VLC installation, place the complete VLC folder as `VLC` beside the EXE. This folder must contain at least `libvlc.dll` and the `plugins` directory.

## Controls

- `▶` and `Ⅱ` start and pause playback; `■` stops it.
- The large `◀◀` and `▶▶` buttons change tracks.
- `🔀` enables or disables shuffle. When active, the button is highlighted; every track is played once in a shuffled order before the next pass starts.
- `🔁` cycles through off, repeat entire playlist, and repeat current track. `🔂` is shown when a single track is repeated.
- `⛶`, `F11`, or `Esc` control full screen. `☰` shows or hides the playlist in windowed mode.
- `🔊` and `🔇` control sound; volume controls are beside them.
- `⌫` removes the selected entry. `🗑` stops playback and clears the entire playlist.

Transport controls are grouped on the left, while mode and volume controls are aligned to the right. The large custom title bar replaces the small Windows controls: drag the title area to move the window, or double-click it to maximize or restore. The window remains resizable at its edges, and maximizing keeps the Windows taskbar visible.

Active modes are highlighted. A short status message appears after a mode is changed. Tooltips, dialogs, error messages, and accessible names automatically use German or English according to the Windows display language.

Playlists exist only for the current session and are discarded when the application closes.

## Playlists

Supported formats are `.xspf`, `.m3u`, `.m3u8`, `.pls`, `.asx`, `.wpl`, `.vlc`, and `.ram`. Relative paths, local files, and stream addresses are preserved in playlist order. An HLS file (`.m3u8` with `#EXT-X-` entries) is passed to VLC as a stream.

Only open playlists from trusted sources: they may contain stream addresses, and VLC can therefore establish a network connection during playback. Network file paths inside playlists are skipped during import. XML playlists are read without DTDs or external XML entities; text playlists are limited to 10 MB and 10,000 entries.

## Privacy and logs

The application collects no telemetry and has no update function.

A `TouchPlayer.log` runtime log may be created next to the EXE. It contains technical start-up and shut-down information and should not be included in an issue or release archive.

## Development

Instructions for local builds, tests, and creating a release archive are in [BUILDING.md](BUILDING.md). The current version's changes are listed in [CHANGELOG.md](CHANGELOG.md). Use the [release checklist](RELEASE_CHECKLIST.md) before publishing.

## Dependency and trademarks

VLC is not bundled. VLC is a project and trademark of VideoLAN. VLC licensing terms are available in the [VLC source code](https://code.videolan.org/videolan/vlc/-/blob/master/COPYING). This project dynamically calls the locally installed VLC library and includes no VLC binaries.

Surface Touch Mediaplayer is licensed under the [MIT License](LICENSE). It applies to this project's own source code, not to VLC or its components.
