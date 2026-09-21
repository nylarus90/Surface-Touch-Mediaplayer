# Surface Touch Mediaplayer

[Deutsche Version](README.md)

Surface Touch Mediaplayer provides a large, touch-friendly Windows interface for an installed VLC playback engine. It is designed for the Surface Pro 8 at 2880 × 1920 pixels with 200% Windows display scaling.

The application includes neither VLC nor media codecs. It automatically locates an existing 64-bit VLC installation.

## Getting started

1. Install the 64-bit version of [VLC media player](https://www.videolan.org/vlc/).
2. Download `Surface Touch Mediaplayer.exe` from the release assets.
3. Double-click the EXE, then open media with `+ FILE` or drag files into the window.

The EXE needs no installation and can be stored anywhere. It searches first in the standard VLC installation directory, then in Windows Registry entries, and finally beside the EXE or in a `VLC` subdirectory. A 32-bit VLC installation is not supported.

For a portable VLC installation, place the complete VLC folder as `VLC` beside the EXE. This folder must contain at least `libvlc.dll` and the `plugins` directory.

## Controls

- `▶` and `Ⅱ` start and pause playback; `■` stops it.
- The large `◀◀` and `▶▶` buttons change tracks.
- `🔀` enables or disables shuffle. When active, the button is highlighted; every track is played once in a shuffled order before the next pass starts.
- `🔁` cycles through off, repeat entire playlist, and repeat current track. `🔂` is shown when a single track is repeated.
- `⛶`, `F11`, or `Esc` control full screen. `HIDE LIST` hides the playlist in windowed mode too.
- `🔊` and `🔇` control sound; volume controls are beside them.
- `DEL` removes the selected entry. `CLEAR` stops playback and clears the entire playlist.

Playlists exist only for the current session and are discarded when the application closes.

## Playlists

Supported formats are `.xspf`, `.m3u`, `.m3u8`, `.pls`, `.asx`, `.wpl`, `.vlc`, and `.ram`. Relative paths, local files, and stream addresses are preserved in playlist order. An HLS file (`.m3u8` with `#EXT-X-` entries) is passed to VLC as a stream.

Only open playlists from trusted sources: they may contain stream addresses, and VLC can therefore establish a network connection during playback. Network file paths inside playlists are skipped during import. XML playlists are read without DTDs or external XML entities; text playlists are limited to 10 MB and 10,000 entries.

## Security and Windows notice

The application collects no telemetry and has no update function. It is currently not signed with a code-signing certificate, so Windows SmartScreen may display a warning on first launch. Verify the SHA-256 checksum in the GitHub release notes or build the EXE from source yourself.

A `TouchPlayer.log` runtime log may be created next to the EXE. It contains technical start-up and shut-down information and should not be included in an issue or release archive.

## Development

Instructions for local builds, tests, and creating a release archive are in [BUILDING.md](BUILDING.md). The current version's changes are listed in [CHANGELOG.md](CHANGELOG.md). Use the [release checklist](RELEASE_CHECKLIST.md) before publishing.

## Dependency and trademarks

VLC is not bundled. VLC is a project and trademark of VideoLAN. VLC licensing terms are available in the [VLC source code](https://code.videolan.org/videolan/vlc/-/blob/master/COPYING). This project dynamically calls the locally installed VLC library and includes no VLC binaries.

Surface Touch Mediaplayer is licensed under the [MIT License](LICENSE). It applies to this project's own source code, not to VLC or its components.
