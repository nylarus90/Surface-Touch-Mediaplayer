using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class PlaylistFormatsTest
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Write(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    [STAThread]
    private static int Main()
    {
        CultureInfo originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
        string root = Path.Combine(Path.GetTempPath(), "vlc-touch-playlist-test-" + Guid.NewGuid().ToString("N"));
        string media = Path.Combine(root, "media");
        Directory.CreateDirectory(media);
        string local = Path.Combine(media, "Überblick.mp4");
        File.WriteAllBytes(local, new byte[] { 0 });
        try {
            string xspf = Path.Combine(root, "list.xspf");
            Write(xspf, "<?xml version=\"1.0\"?><playlist xmlns=\"http://xspf.org/ns/0/\" version=\"1\" xml:base=\"media/\"><trackList><track><title>Film</title><location>Überblick.mp4</location></track><track><location>" + new Uri(local).AbsoluteUri + "</location></track><track><location>https://example.org/live</location></track></trackList></playlist>");
            var x = PlaylistReader.Read(xspf);
            Check(x.Count == 3 && x[0].Source == local && x[0].Title == "Film" && x[1].Source == local && x[2].Source == "https://example.org/live", "XSPF");

            string m3u = Path.Combine(root, "list.m3u");
            Write(m3u, "#EXTM3U\n#EXTINF:-1,Mein Film\nmedia/Überblick.mp4\nhttps://example.org/radio\n");
            var m = PlaylistReader.Read(m3u);
            Check(m.Count == 2 && m[0].Source == local && m[0].Title == "Mein Film" && m[1].Source == "https://example.org/radio", "M3U");

            string m3u8 = Path.Combine(root, "list.m3u8");
            Write(m3u8, "#EXTM3U\n#EXTINF:-1,Überblick\nmedia/Überblick.mp4\n");
            var m8 = PlaylistReader.Read(m3u8);
            Check(m8.Count == 1 && m8[0].Source == local && m8[0].Title == "Überblick", "M3U8 UTF-8");

            string hls = Path.Combine(root, "stream.m3u8");
            Write(hls, "#EXTM3U\n#EXT-X-TARGETDURATION:4\n#EXTINF:4,\nsegment.ts\n");
            var h = PlaylistReader.Read(hls);
            Check(h.Count == 1 && h[0].Source == hls, "HLS M3U8");

            string pls = Path.Combine(root, "list.pls");
            Write(pls, "[playlist]\nFile2=https://example.org/radio\nTitle2=Radio\nFile1=media/Überblick.mp4\nTitle1=Film\nNumberOfEntries=2\n");
            var p = PlaylistReader.Read(pls);
            Check(p.Count == 2 && p[0].Source == local && p[0].Title == "Film" && p[1].Title == "Radio", "PLS order");

            string asx = Path.Combine(root, "list.asx");
            Write(asx, "<ASX version=\"3.0\"><ENTRY><TITLE>Film</TITLE><REF HREF=\"media/Überblick.mp4\" /></ENTRY></ASX>");
            var a = PlaylistReader.Read(asx);
            Check(a.Count == 1 && a[0].Source == local && a[0].Title == "Film", "ASX");

            string wpl = Path.Combine(root, "list.wpl");
            Write(wpl, "<smil><body><seq><media src=\"media/Überblick.mp4\" /></seq></body></smil>");
            var w = PlaylistReader.Read(wpl);
            Check(w.Count == 1 && w[0].Source == local, "WPL");

            string vlc = Path.Combine(root, "list.vlc");
            Write(vlc, "media/Überblick.mp4\n");
            Check(PlaylistReader.Read(vlc)[0].Source == local, "VLC text list");
            string ram = Path.Combine(root, "list.ram");
            Write(ram, "https://example.org/live\n");
            Check(PlaylistReader.Read(ram)[0].Source == "https://example.org/live", "RAM text list");

            string evil = Path.Combine(root, "evil.xspf");
            Write(evil, "<!DOCTYPE playlist [<!ENTITY x SYSTEM \"file:///C:/Windows/win.ini\">]><playlist><trackList><track><location>&x;</location></track></trackList></playlist>");
            bool rejected = false;
            try { PlaylistReader.Read(evil); } catch (System.Xml.XmlException) { rejected = true; }
            Check(rejected, "External XML entity rejected");

            string blockedNetwork = Path.Combine(root, "blocked-network.m3u");
            Write(blockedNetwork, "\\\\example.invalid\\share\\movie.mp4\n");
            Check(PlaylistReader.Read(blockedNetwork).Count == 0, "Netzwerkpfade beim Listenimport blockieren");

            string utf16 = Path.Combine(root, "utf16.m3u");
            File.WriteAllText(utf16, "#EXTM3U\r\n#EXTINF:-1,UTF-16 Film\r\nmedia/Überblick.mp4\r\n", Encoding.Unicode);
            var utf16List = PlaylistReader.Read(utf16);
            Check(utf16List.Count == 1 && utf16List[0].Title == "UTF-16 Film" && utf16List[0].Source == local, "UTF-16 M3U");

            string fallback = Path.Combine(root, "fallback.xspf");
            Write(fallback, "<playlist><trackList><track><location>media/fehlt.mp4</location><location>media/Überblick.mp4</location></track></trackList></playlist>");
            var fallbackList = PlaylistReader.Read(fallback);
            Check(fallbackList.Count == 1 && fallbackList[0].Source == local, "XSPF-Alternativquelle");

            string portableVlc = Path.Combine(root, "portable-vlc");
            Directory.CreateDirectory(Path.Combine(portableVlc, "plugins"));
            byte[] x64Library = new byte[0x90];
            x64Library[0] = (byte)'M'; x64Library[1] = (byte)'Z';
            BitConverter.GetBytes(0x80).CopyTo(x64Library, 0x3C);
            x64Library[0x80] = (byte)'P'; x64Library[0x81] = (byte)'E';
            BitConverter.GetBytes((ushort)0x8664).CopyTo(x64Library, 0x84);
            File.WriteAllBytes(Path.Combine(portableVlc, "libvlc.dll"), x64Library);
            File.WriteAllBytes(Path.Combine(portableVlc, "libvlccore.dll"), x64Library);
            Check(VlcInstallation.FirstValidDirectory(new string[] { Path.Combine(root, "missing-vlc"), portableVlc }) == Path.GetFullPath(portableVlc), "VLC-Installationssuche");
            Check(VlcInstallation.FirstValidDirectory(new string[] { Path.Combine(root, "missing-vlc") }) == null, "Ungültige VLC-Installation ablehnen");

            TouchPlayer form = new TouchPlayer();
            try {
                typeof(TouchPlayer).GetMethod("AddFiles", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, new object[] { new string[] { xspf, m3u, m3u8, hls, pls, asx, wpl, vlc, ram } });
                ListBox list = (ListBox)typeof(TouchPlayer).GetField("playlist", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Check(list.Items.Count == 13 && list.Items[0].ToString() == "Film", "Dateiauswahl und Drag-and-drop Import");

                Button play = (Button)typeof(TouchPlayer).GetField("playPause", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button previous = (Button)typeof(TouchPlayer).GetField("previousButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button next = (Button)typeof(TouchPlayer).GetField("nextButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Label title = (Label)typeof(TouchPlayer).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                TableLayoutPanel layout = (TableLayoutPanel)typeof(TouchPlayer).GetField("root", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                TableLayoutPanel middle = (TableLayoutPanel)typeof(TouchPlayer).GetField("middle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Panel video = (Panel)typeof(TouchPlayer).GetField("video", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button listButton = (Button)typeof(TouchPlayer).GetField("playlistButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button openButton = (Button)typeof(TouchPlayer).GetField("openButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button removeButton = (Button)typeof(TouchPlayer).GetField("removeButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button shuffleButton = (Button)typeof(TouchPlayer).GetField("shuffleButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button repeatButton = (Button)typeof(TouchPlayer).GetField("repeatButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button muteButton = (Button)typeof(TouchPlayer).GetField("muteButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Button clearButton = (Button)typeof(TouchPlayer).GetField("clearButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Label volume = (Label)typeof(TouchPlayer).GetField("volume", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                MethodInfo toggleList = typeof(TouchPlayer).GetMethod("TogglePlaylist", BindingFlags.NonPublic | BindingFlags.Instance);
                Check(play.Height >= 104 && play.Font.Size >= 16, "Große Wiedergabetasten");
                Check(previous.Width >= 170 && next.Width >= 170 && previous.Height >= 112 && next.Height >= 112 && previous.Font.Size >= 22 && next.Font.Size >= 22, "Extra große Vor- und Zurücktasten");
                Check(muteButton.Width >= 118 && volume.Width >= 120, "Breite Ton- und Lautstärkeanzeige");
                Check(openButton.Text == "＋" && openButton.AccessibleName == "Dateien öffnen", "Sprachneutrale Öffnen-Taste");
                Check(removeButton.Text == "⌫" && removeButton.AccessibleName == "Markierten Eintrag entfernen", "Sprachneutrale Entfernen-Taste");
                Check(clearButton.Width >= 100 && clearButton.Text == "🗑" && clearButton.AccessibleName == "Wiedergabeliste leeren", "Sprachneutrale Taste zum Leeren der Wiedergabeliste");
                Check(layout.RowStyles[2].Height >= 260 && title.Font.Size >= 17, "Titel- und Bedienbereich");
                toggleList.Invoke(form, null);
                Check(middle.ColumnStyles[1].Width == 0 && video.Margin.Right == 0 && listButton.Text == "☰" && listButton.AccessibleName.Contains("ausgeblendet"), "Liste ausblenden");
                toggleList.Invoke(form, null);
                Check(middle.ColumnStyles[1].Width == 30 && video.Margin.Right > 0 && listButton.Text == "☰" && listButton.AccessibleName.Contains("sichtbar"), "Liste einblenden");

                MethodInfo toggleShuffle = typeof(TouchPlayer).GetMethod("ToggleShuffle", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo cycleRepeat = typeof(TouchPlayer).GetMethod("CycleRepeatMode", BindingFlags.NonPublic | BindingFlags.Instance);
                toggleShuffle.Invoke(form, null);
                Check(shuffleButton.Text == "🔀" && (bool)typeof(TouchPlayer).GetField("shuffleEnabled", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form), "Zufallswiedergabe einschalten");
                object remaining = typeof(TouchPlayer).GetField("shuffleRemaining", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                Check((int)remaining.GetType().GetProperty("Count").GetValue(remaining, null) == list.Items.Count, "Zufälligen Durchlauf vorbereiten");
                cycleRepeat.Invoke(form, null);
                Check(repeatButton.Text == "🔁" && typeof(TouchPlayer).GetField("repeatMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form).ToString() == "All", "Alle Titel wiederholen");
                cycleRepeat.Invoke(form, null);
                Check(repeatButton.Text == "🔂" && typeof(TouchPlayer).GetField("repeatMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form).ToString() == "One", "Einen Titel wiederholen");
                cycleRepeat.Invoke(form, null);
                Check(repeatButton.Text == "🔁" && typeof(TouchPlayer).GetField("repeatMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form).ToString() == "Off", "Wiederholung ausschalten");
                toggleShuffle.Invoke(form, null);
                Check(shuffleButton.Text == "🔀" && !(bool)typeof(TouchPlayer).GetField("shuffleEnabled", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form), "Zufallswiedergabe ausschalten");

                typeof(TouchPlayer).GetMethod("ClearPlaylist", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, null);
                Check(list.Items.Count == 0 && title.Text == "Datei öffnen oder hierher ziehen", "Wiedergabeliste vollständig leeren");

                typeof(TouchPlayer).GetMethod("OnClosing", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(object), typeof(FormClosingEventArgs) }, null).Invoke(form, new object[] { form, new FormClosingEventArgs(CloseReason.None, false) });
            } finally { form.Dispose(); }

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            TouchPlayer englishForm = new TouchPlayer();
            try {
                Label englishTitle = (Label)typeof(TouchPlayer).GetField("title", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(englishForm);
                Button englishOpen = (Button)typeof(TouchPlayer).GetField("openButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(englishForm);
                Button englishClear = (Button)typeof(TouchPlayer).GetField("clearButton", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(englishForm);
                Check(englishTitle.Text == "Open a file or drag it here", "Englischer Leerzustand");
                Check(englishOpen.AccessibleName == "Open files" && englishClear.AccessibleName == "Clear playlist", "Englische Tooltips und zugängliche Namen");
            } finally { englishForm.Dispose(); }

            Console.WriteLine("Playlist-Formate, Sitzungsmodus, Wiederholen, Zufall und Oberfläche: OK");
            return 0;
        } catch (Exception ex) {
            Console.Error.WriteLine("TEST FEHLGESCHLAGEN: " + ex.Message);
            return 1;
        } finally {
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }
}
