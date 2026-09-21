using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

internal static class VlcNative
{
    private const string Dll = "libvlc.dll";
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern bool SetDllDirectory(string path);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr libvlc_new(int argc, IntPtr argv);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_release(IntPtr instance);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr libvlc_media_player_new(IntPtr instance);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_player_release(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr libvlc_media_new_path(IntPtr instance, IntPtr path);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr libvlc_media_new_location(IntPtr instance, IntPtr location);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_release(IntPtr media);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_player_set_media(IntPtr player, IntPtr media);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_player_set_hwnd(IntPtr player, IntPtr hwnd);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int libvlc_media_player_play(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_player_set_pause(IntPtr player, int paused);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_player_stop(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int libvlc_media_player_get_state(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern long libvlc_media_player_get_time(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern long libvlc_media_player_get_length(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_media_player_set_time(IntPtr player, long time);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int libvlc_audio_get_volume(IntPtr player);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int libvlc_audio_set_volume(IntPtr player, int volume);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void libvlc_audio_toggle_mute(IntPtr player);
}

internal static class VlcInstallation
{
    internal static string FindDirectory()
    {
        List<string> candidates = new List<string>();
        string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLAN", "VLC"));
        AddRegistryCandidates(candidates);
        candidates.Add(appDirectory);
        candidates.Add(Path.Combine(appDirectory, "VLC"));
        return FirstValidDirectory(candidates);
    }

    internal static string FirstValidDirectory(IEnumerable<string> candidates)
    {
        foreach (string candidate in candidates) {
            if (String.IsNullOrWhiteSpace(candidate)) continue;
            try {
                string directory = Path.GetFullPath(candidate);
                string libvlc = Path.Combine(directory, "libvlc.dll");
                if (File.Exists(libvlc) && File.Exists(Path.Combine(directory, "libvlccore.dll")) && Directory.Exists(Path.Combine(directory, "plugins")) && Is64BitLibrary(libvlc)) return directory;
            } catch (Exception) { }
        }
        return null;
    }

    private static bool Is64BitLibrary(string path)
    {
        try {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (stream.Length < 0x40) return false;
                byte[] header = new byte[64];
                if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 'M' || header[1] != 'Z') return false;
                int peOffset = BitConverter.ToInt32(header, 0x3C);
                if (peOffset < 0 || peOffset > stream.Length - 6) return false;
                stream.Position = peOffset;
                byte[] peHeader = new byte[6];
                if (stream.Read(peHeader, 0, peHeader.Length) != peHeader.Length) return false;
                return peHeader[0] == 'P' && peHeader[1] == 'E' && peHeader[2] == 0 && peHeader[3] == 0 && BitConverter.ToUInt16(peHeader, 4) == 0x8664;
            }
        } catch (Exception) { return false; }
    }

    private static void AddRegistryCandidates(List<string> candidates)
    {
        AddRegistryDirectory(candidates, Registry.CurrentUser, @"SOFTWARE\VideoLAN\VLC", "InstallDir");
        AddRegistryDirectory(candidates, Registry.LocalMachine, @"SOFTWARE\VideoLAN\VLC", "InstallDir");
        AddRegistryExecutableDirectory(candidates, Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vlc.exe");
        AddRegistryExecutableDirectory(candidates, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vlc.exe");
    }

    private static void AddRegistryDirectory(List<string> candidates, RegistryKey hive, string subKey, string valueName)
    {
        try {
            using (RegistryKey key = hive.OpenSubKey(subKey)) {
                object value = key == null ? null : key.GetValue(valueName);
                if (value is string) candidates.Add((string)value);
            }
        } catch (Exception) { }
    }

    private static void AddRegistryExecutableDirectory(List<string> candidates, RegistryKey hive, string subKey)
    {
        try {
            using (RegistryKey key = hive.OpenSubKey(subKey)) {
                object value = key == null ? null : key.GetValue("");
                if (value is string && File.Exists((string)value)) candidates.Add(Path.GetDirectoryName((string)value));
            }
        } catch (Exception) { }
    }
}

internal sealed class PlaylistEntry
{
    internal readonly string Source;
    internal readonly string Title;
    internal PlaylistEntry(string source, string title) { Source = source; Title = title; }
}

internal static class PlaylistReader
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    private const long MaxPlaylistBytes = 10 * 1024 * 1024;
    private const int MaxPlaylistEntries = 10000;
    private const int MaxTitleLength = 512;
    internal static bool IsPlaylist(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".xspf" || ext == ".m3u" || ext == ".m3u8" || ext == ".pls" || ext == ".asx" || ext == ".wpl" || ext == ".vlc" || ext == ".ram";
    }

    internal static List<PlaylistEntry> Read(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".xspf") return ReadXspf(path);
        if (ext == ".m3u" || ext == ".m3u8" || ext == ".vlc" || ext == ".ram") return ReadM3u(path);
        if (ext == ".pls") return ReadPls(path);
        if (ext == ".asx") return ReadAsx(path);
        if (ext == ".wpl") return ReadWpl(path);
        throw new FormatException("Dieses Wiedergabelistenformat wird nicht unterstützt.");
    }

    private static XmlDocument LoadXml(string path)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        settings.MaxCharactersInDocument = 20000000;
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        using (XmlReader reader = XmlReader.Create(path, settings)) document.Load(reader);
        return document;
    }

    private static List<PlaylistEntry> ReadXspf(string path)
    {
        XmlDocument document = LoadXml(path);
        if (document.DocumentElement == null || document.DocumentElement.LocalName != "playlist")
            throw new FormatException("Die Datei ist keine XSPF-Wiedergabeliste.");
        List<PlaylistEntry> result = new List<PlaylistEntry>();
        XmlNodeList tracks = document.DocumentElement.SelectNodes("./*[local-name()='trackList']/*[local-name()='track']");
        foreach (XmlNode track in tracks) {
            XmlNode titleNode = track.SelectSingleNode("./*[local-name()='title']");
            string title = titleNode == null ? "" : titleNode.InnerText.Trim();
            XmlNodeList locations = track.SelectNodes("./*[local-name()='location']");
            foreach (XmlNode location in locations) {
                string source = Resolve(location, path);
                if (source.Length == 0) continue;
                if (Path.IsPathRooted(source) && !File.Exists(source)) continue;
                AddResult(result, source, title);
                break;
            }
        }
        return result;
    }

    private static string Resolve(XmlNode location, string path)
    {
        string value = location.InnerText.Trim();
        if (value.Length == 0) return "";
        if (Path.IsPathRooted(value) && !value.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) {
            string localPath = Path.GetFullPath(value);
            return IsNetworkPath(localPath) ? "" : localPath;
        }
        Uri baseUri = new Uri(Path.GetFullPath(path));
        List<XmlNode> ancestors = new List<XmlNode>();
        for (XmlNode node = location; node != null && node.NodeType == XmlNodeType.Element; node = node.ParentNode)
            ancestors.Add(node);
        ancestors.Reverse();
        foreach (XmlNode node in ancestors) {
            XmlAttribute xmlBase = node.Attributes == null ? null : node.Attributes["base", XmlNamespace];
            if (xmlBase != null && xmlBase.Value.Trim().Length > 0)
                baseUri = new Uri(baseUri, xmlBase.Value.Trim());
        }
        return ResolveText(value, baseUri);
    }

    private static string ResolveText(string value, string playlistPath)
    {
        return ResolveText(value, new Uri(Path.GetFullPath(playlistPath)));
    }

    private static string ResolveText(string value, Uri baseUri)
    {
        value = value.Trim();
        if (value.Length == 0) return "";
        if (Path.IsPathRooted(value) && !value.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) {
            string localPath = Path.GetFullPath(value);
            return IsNetworkPath(localPath) ? "" : localPath;
        }
        Uri resolved;
        if (!Uri.TryCreate(baseUri, value.Replace('\\', '/'), out resolved)) return "";
        return resolved.IsFile ? (IsNetworkPath(resolved.LocalPath) ? "" : resolved.LocalPath) : resolved.AbsoluteUri;
    }

    private static bool IsNetworkPath(string path)
    {
        try {
            Uri uri = new Uri(path);
            if (uri.IsUnc) return true;
            string root = Path.GetPathRoot(path);
            return root != null && new DriveInfo(root).DriveType == DriveType.Network;
        } catch (Exception) { return false; }
    }

    private static void AddResult(List<PlaylistEntry> result, string source, string title)
    {
        if (result.Count >= MaxPlaylistEntries) throw new FormatException("Die Wiedergabeliste enthält mehr als " + MaxPlaylistEntries + " Einträge.");
        title = title == null ? "" : title.Trim();
        if (title.Length > MaxTitleLength) title = title.Substring(0, MaxTitleLength);
        result.Add(new PlaylistEntry(source, title));
    }

    private static string[] ReadLines(string path)
    {
        FileInfo info = new FileInfo(path);
        if (info.Length > MaxPlaylistBytes) throw new FormatException("Die Wiedergabeliste ist größer als 10 MB.");
        byte[] bytes = File.ReadAllBytes(path);
        string text;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) text = Encoding.Unicode.GetString(bytes);
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) text = Encoding.BigEndianUnicode.GetString(bytes);
        else {
            try { text = new UTF8Encoding(false, true).GetString(bytes); }
            catch (DecoderFallbackException) { text = Encoding.Default.GetString(bytes); }
        }
        return text.TrimStart('\uFEFF').Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    }

    private static List<PlaylistEntry> ReadM3u(string path)
    {
        string[] lines = ReadLines(path);
        foreach (string raw in lines)
            if (raw.TrimStart().StartsWith("#EXT-X-", StringComparison.OrdinalIgnoreCase))
                return new List<PlaylistEntry> { new PlaylistEntry(Path.GetFullPath(path), Path.GetFileName(path)) };
        List<PlaylistEntry> result = new List<PlaylistEntry>();
        string pendingTitle = "";
        foreach (string raw in lines) {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase)) {
                int comma = line.IndexOf(',');
                pendingTitle = comma >= 0 ? line.Substring(comma + 1).Trim() : "";
                continue;
            }
            if (line.StartsWith("#") || line.StartsWith(";")) continue;
            string source = ResolveText(line, path);
            if (source.Length > 0) AddResult(result, source, pendingTitle);
            pendingTitle = "";
        }
        return result;
    }

    private static List<PlaylistEntry> ReadPls(string path)
    {
        Dictionary<int, string> sources = new Dictionary<int, string>();
        Dictionary<int, string> titles = new Dictionary<int, string>();
        foreach (string raw in ReadLines(path)) {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
            int equals = line.IndexOf('=');
            if (equals < 0) continue;
            string key = line.Substring(0, equals).Trim();
            string value = line.Substring(equals + 1).Trim();
            int number;
            if (key.StartsWith("File", StringComparison.OrdinalIgnoreCase) && int.TryParse(key.Substring(4), out number))
                sources[number] = value;
            else if (key.StartsWith("Title", StringComparison.OrdinalIgnoreCase) && int.TryParse(key.Substring(5), out number))
                titles[number] = value;
        }
        List<int> keys = new List<int>(sources.Keys);
        keys.Sort();
        List<PlaylistEntry> result = new List<PlaylistEntry>();
        foreach (int number in keys) {
            string source = ResolveText(sources[number], path);
            if (source.Length > 0) AddResult(result, source, titles.ContainsKey(number) ? titles[number] : "");
        }
        return result;
    }

    private static List<PlaylistEntry> ReadAsx(string path)
    {
        XmlDocument document = LoadXml(path);
        if (document.DocumentElement == null || document.DocumentElement.LocalName.ToLowerInvariant() != "asx")
            throw new FormatException("Die Datei ist keine ASX-Wiedergabeliste.");
        List<PlaylistEntry> result = new List<PlaylistEntry>();
        XmlNodeList entries = document.DocumentElement.SelectNodes("./*[translate(local-name(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='entry']");
        foreach (XmlNode entry in entries) {
            XmlNode titleNode = entry.SelectSingleNode("./*[translate(local-name(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='title']");
            string title = titleNode == null ? "" : titleNode.InnerText.Trim();
            XmlNodeList refs = entry.SelectNodes("./*[translate(local-name(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='ref']");
            foreach (XmlNode reference in refs) {
                string href = AttributeValue(reference, "href");
                if (href.Length == 0) continue;
                string source = ResolveText(href, path);
                if (source.Length == 0 || (Path.IsPathRooted(source) && !File.Exists(source))) continue;
                AddResult(result, source, title);
                break;
            }
        }
        return result;
    }

    private static List<PlaylistEntry> ReadWpl(string path)
    {
        XmlDocument document = LoadXml(path);
        if (document.DocumentElement == null || document.DocumentElement.LocalName.ToLowerInvariant() != "smil")
            throw new FormatException("Die Datei ist keine WPL-Wiedergabeliste.");
        List<PlaylistEntry> result = new List<PlaylistEntry>();
        XmlNodeList media = document.DocumentElement.SelectNodes(".//*[translate(local-name(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='media']");
        foreach (XmlNode item in media) {
            string rawSource = AttributeValue(item, "src");
            if (rawSource.Length == 0) continue;
            string source = ResolveText(rawSource, path);
            if (source.Length > 0) AddResult(result, source, "");
        }
        return result;
    }

    private static string AttributeValue(XmlNode node, string name)
    {
        if (node.Attributes == null) return "";
        foreach (XmlAttribute attribute in node.Attributes)
            if (String.Equals(attribute.LocalName, name, StringComparison.OrdinalIgnoreCase)) return attribute.Value;
        return "";
    }
}

internal sealed class SeekBar : Control
{
    private double fraction;
    private bool dragging;
    internal event Action<double> SeekRequested;
    internal double Fraction
    {
        get { return fraction; }
        set { fraction = Math.Max(0, Math.Min(1, value)); Invalidate(); }
    }
    internal bool IsDragging { get { return dragging; } }
    internal SeekBar()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        MinimumSize = new Size(100, 40);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int y = Height / 2;
        using (Brush baseBrush = new SolidBrush(Color.FromArgb(58, 77, 99)))
            e.Graphics.FillRectangle(baseBrush, 12, y - 8, Math.Max(1, Width - 24), 16);
        int end = 12 + (int)Math.Round(Math.Max(1, Width - 24) * fraction);
        using (Brush progress = new SolidBrush(Color.FromArgb(101, 214, 203)))
            e.Graphics.FillRectangle(progress, 12, y - 8, Math.Max(0, end - 12), 16);
        using (Brush thumb = new SolidBrush(Color.White))
            e.Graphics.FillEllipse(thumb, end - 18, y - 18, 36, 36);
    }
    private void UpdateFromX(int x)
    {
        Fraction = (double)(x - 12) / Math.Max(1, Width - 24);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { dragging = true; Capture = true; UpdateFromX(e.X); }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging) UpdateFromX(e.X);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (dragging) { UpdateFromX(e.X); dragging = false; Capture = false; if (SeekRequested != null) SeekRequested(Fraction); }
    }
}

internal sealed class TouchPlayer : Form
{
    private enum RepeatMode { Off, All, One }

    private readonly Color bg = Color.FromArgb(16, 24, 39);
    private readonly Color panel = Color.FromArgb(25, 38, 56);
    private readonly Color button = Color.FromArgb(41, 65, 89);
    private readonly Color accent = Color.FromArgb(101, 214, 203);
    private readonly List<string> files = new List<string>();
    private readonly Panel video = new Panel();
    private readonly TableLayoutPanel root = new TableLayoutPanel();
    private readonly TableLayoutPanel middle = new TableLayoutPanel();
    private readonly ListBox playlist = new ListBox();
    private readonly SeekBar seek = new SeekBar();
    private readonly Label title = new Label();
    private readonly Label time = new Label();
    private readonly Label volume = new Label();
    private readonly Button previousButton;
    private readonly Button nextButton;
    private readonly Button playPause;
    private readonly Button maximize;
    private readonly Button fullButton;
    private readonly Button playlistButton;
    private readonly Button shuffleButton;
    private readonly Button repeatButton;
    private readonly Button muteButton;
    private readonly Button clearButton;
    private readonly Panel top = new Panel();
    private readonly Panel side = new Panel();
    private readonly Timer timer = new Timer();
    private readonly ToolTip tips = new ToolTip();
    private readonly Random random = new Random();
    private readonly List<int> shuffleRemaining = new List<int>();
    private readonly Stack<int> shuffleHistory = new Stack<int>();
    private IntPtr instance;
    private IntPtr player;
    private int current = -1;
    private bool endedHandled;
    private int lastState = -1;
    private bool fullscreen;
    private bool playlistVisible = true;
    private bool shuffleEnabled;
    private bool muted;
    private bool closingInBackground;
    private bool resourcesReleased;
    private RepeatMode repeatMode;
    private FormWindowState savedState;
    private Rectangle savedBounds;

    private void Log(string message)
    {
        try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TouchPlayer.log"), DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine); } catch { }
    }

    private int U(int value) { return Math.Max(1, (int)Math.Round(value * DeviceDpi / 96.0)); }

    private Button MakeButton(string label, int width, EventHandler click)
    {
        Button result = new Button();
        result.Text = label;
        result.Width = U(width);
        result.Height = U(72);
        result.Margin = new Padding(U(5));
        result.FlatStyle = FlatStyle.Flat;
        result.FlatAppearance.BorderColor = Color.FromArgb(75, 105, 128);
        result.BackColor = button;
        result.ForeColor = Color.White;
        result.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        result.Click += click;
        return result;
    }

    private Button MakeTransportButton(string label, int width, EventHandler click)
    {
        Button result = MakeButton(label, width, click);
        result.Height = U(104);
        result.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        result.Margin = new Padding(U(5));
        return result;
    }

    private Button MakeNavigationButton(string label, EventHandler click)
    {
        Button result = MakeTransportButton(label, 170, click);
        result.Height = U(112);
        result.Font = new Font("Segoe UI Symbol", 22, FontStyle.Bold);
        return result;
    }

    private Button MakeMediaIconButton(string symbol, int width, string tooltip, EventHandler click)
    {
        Button result = MakeTransportButton(symbol, width, click);
        result.Font = new Font("Segoe UI Symbol", 24, FontStyle.Regular);
        tips.SetToolTip(result, tooltip);
        return result;
    }

    private Label MakeLabel(string text, int points, Color color)
    {
        Label result = new Label();
        result.Text = text;
        result.ForeColor = color;
        result.Font = new Font("Segoe UI", points, FontStyle.Regular);
        result.AutoEllipsis = true;
        result.TextAlign = ContentAlignment.MiddleLeft;
        return result;
    }

    internal TouchPlayer()
    {
        Text = "Surface Touch Mediaplayer";
        BackColor = bg;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 13);
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(U(900), U(650));
        Rectangle work = Screen.PrimaryScreen.WorkingArea;
        Size = new Size(Math.Min(work.Width, U(1400)), Math.Min(work.Height, U(900)));
        root.Dock = DockStyle.Fill;
        root.BackColor = bg;
        root.RowCount = 3;
        root.ColumnCount = 1;
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, U(86)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, U(260)));
        Controls.Add(root);

        top.Dock = DockStyle.Fill;
        top.BackColor = panel;
        root.Controls.Add(top, 0, 0);
        FlowLayoutPanel topActions = new FlowLayoutPanel();
        topActions.Dock = DockStyle.Right;
        topActions.Width = U(550);
        topActions.WrapContents = false;
        topActions.FlowDirection = FlowDirection.LeftToRight;
        top.Controls.Add(topActions);
        Button open = MakeButton("+ DATEI", 145, delegate { OpenFiles(); });
        topActions.Controls.Add(open);
        playlistButton = MakeButton("LISTE AUS", 130, delegate { TogglePlaylist(); });
        topActions.Controls.Add(playlistButton);
        topActions.Controls.Add(MakeButton("—", 65, delegate { WindowState = FormWindowState.Minimized; }));
        maximize = MakeButton("□", 65, delegate { ToggleMaximize(); });
        topActions.Controls.Add(maximize);
        topActions.Controls.Add(MakeButton("X", 65, delegate { Close(); }));
        Label brand = MakeLabel("SURFACE TOUCH  ·  MEDIAPLAYER", 22, Color.White);
        brand.Dock = DockStyle.Fill;
        brand.Padding = new Padding(U(20), 0, 0, 0);
        top.Controls.Add(brand);
        brand.SendToBack();

        middle.Dock = DockStyle.Fill;
        middle.Padding = new Padding(U(12));
        middle.ColumnCount = 2;
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        root.Controls.Add(middle, 0, 1);
        video.Dock = DockStyle.Fill;
        video.BackColor = Color.FromArgb(7, 13, 23);
        video.Margin = new Padding(0, 0, U(12), 0);
        middle.Controls.Add(video, 0, 0);
        side.Dock = DockStyle.Fill;
        side.BackColor = panel;
        middle.Controls.Add(side, 1, 0);
        Label listTitle = MakeLabel("WIEDERGABELISTE", 17, Color.White);
        listTitle.Dock = DockStyle.Top;
        listTitle.Height = U(62);
        listTitle.Padding = new Padding(U(16), 0, 0, 0);
        side.Controls.Add(listTitle);
        FlowLayoutPanel listActions = new FlowLayoutPanel();
        listActions.Dock = DockStyle.Bottom;
        listActions.Height = U(86);
        listActions.WrapContents = false;
        side.Controls.Add(listActions);
        listActions.Controls.Add(MakeButton("ENTF.", 100, delegate { RemoveSelected(); }));
        clearButton = MakeButton("LEER", 100, delegate { ClearPlaylist(); });
        tips.SetToolTip(clearButton, "Wiedergabeliste leeren");
        listActions.Controls.Add(clearButton);
        listActions.Controls.Add(MakeButton("↑", 60, delegate { ScrollList(-3); }));
        listActions.Controls.Add(MakeButton("↓", 60, delegate { ScrollList(3); }));
        playlist.Dock = DockStyle.Fill;
        playlist.BackColor = panel;
        playlist.ForeColor = Color.White;
        playlist.BorderStyle = BorderStyle.None;
        playlist.Font = new Font("Segoe UI", 17);
        playlist.DrawMode = DrawMode.OwnerDrawFixed;
        playlist.ItemHeight = U(56);
        playlist.DrawItem += DrawPlaylistItem;
        playlist.DoubleClick += delegate { PlaySelected(); };
        side.Controls.Add(playlist);
        playlist.BringToFront();

        TableLayoutPanel bottom = new TableLayoutPanel();
        bottom.Dock = DockStyle.Fill;
        bottom.BackColor = panel;
        bottom.Padding = new Padding(U(12), U(8), U(12), U(6));
        bottom.RowCount = 3;
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, U(62)));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, U(58)));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(bottom, 0, 2);
        TableLayoutPanel info = new TableLayoutPanel();
        info.Dock = DockStyle.Fill;
        info.ColumnCount = 2;
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        bottom.Controls.Add(info, 0, 0);
        title.Text = "Datei öffnen oder hierher ziehen";
        title.ForeColor = Color.White;
        title.Font = new Font("Segoe UI", 17);
        title.Dock = DockStyle.Fill;
        title.AutoEllipsis = true;
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.Margin = new Padding(0);
        title.Padding = new Padding(U(8), 0, U(8), 0);
        info.Controls.Add(title, 0, 0);
        time.Text = "00:00 / 00:00";
        time.ForeColor = accent;
        time.Font = new Font("Segoe UI", 13);
        time.Dock = DockStyle.Fill;
        time.TextAlign = ContentAlignment.MiddleRight;
        time.Margin = new Padding(0);
        time.Padding = new Padding(U(8), 0, U(8), 0);
        info.Controls.Add(time, 1, 0);
        seek.Dock = DockStyle.Fill;
        seek.SeekRequested += delegate(double fraction) {
            if (player != IntPtr.Zero) {
                long length = VlcNative.libvlc_media_player_get_length(player);
                if (length > 0) VlcNative.libvlc_media_player_set_time(player, (long)(length * fraction));
            }
        };
        bottom.Controls.Add(seek, 0, 1);
        FlowLayoutPanel controls = new FlowLayoutPanel();
        controls.Dock = DockStyle.Fill;
        controls.WrapContents = false;
        bottom.Controls.Add(controls, 0, 2);
        previousButton = MakeNavigationButton("◀◀", delegate { Previous(); });
        controls.Controls.Add(previousButton);
        playPause = MakeMediaIconButton("▶", 132, "Wiedergabe / Pause", delegate { TogglePlayback(); });
        controls.Controls.Add(playPause);
        controls.Controls.Add(MakeMediaIconButton("■", 116, "Stopp", delegate { StopPlayback(); }));
        nextButton = MakeNavigationButton("▶▶", delegate { Next(); });
        controls.Controls.Add(nextButton);
        shuffleButton = MakeMediaIconButton("🔀", 116, "Zufallswiedergabe: aus", delegate { ToggleShuffle(); });
        controls.Controls.Add(shuffleButton);
        repeatButton = MakeMediaIconButton("🔁", 116, "Wiederholen: aus", delegate { CycleRepeatMode(); });
        controls.Controls.Add(repeatButton);
        fullButton = MakeMediaIconButton("⛶", 112, "Vollbild", delegate { ToggleFullscreen(); });
        controls.Controls.Add(fullButton);
        controls.Controls.Add(MakeTransportButton("−", 70, delegate { ChangeVolume(-10); }));
        controls.Controls.Add(MakeTransportButton("+", 70, delegate { ChangeVolume(10); }));
        muteButton = MakeMediaIconButton("🔊", 118, "Ton ein / aus", delegate { ToggleMute(); });
        controls.Controls.Add(muteButton);
        volume.Text = "100%";
        volume.ForeColor = accent;
        volume.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        volume.AutoSize = false;
        volume.Width = U(120);
        volume.Height = U(102);
        volume.TextAlign = ContentAlignment.MiddleCenter;
        controls.Controls.Add(volume);

        AllowDrop = true;
        video.AllowDrop = true;
        playlist.AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        video.DragEnter += OnDragEnter;
        video.DragDrop += OnDragDrop;
        playlist.DragEnter += OnDragEnter;
        playlist.DragDrop += OnDragDrop;
        KeyDown += OnKeyDown;
        Resize += delegate { maximize.Text = WindowState == FormWindowState.Maximized ? "❐" : "□"; };
        FormClosing += OnClosing;
        Shown += OnShown;
        timer.Interval = 500;
        timer.Tick += OnTick;
    }

    private void OnShown(object sender, EventArgs e)
    {
        Log("Shown");
        try {
            string vlcDir = VlcInstallation.FindDirectory();
            if (vlcDir == null) throw new InvalidOperationException("VLC wurde nicht gefunden. Installiere die 64-Bit-Version von VLC oder lege eine portable VLC-Installation im Unterordner 'VLC' neben dieser EXE ab.");
            if (!VlcNative.SetDllDirectory(vlcDir)) throw new InvalidOperationException("Der VLC-Installationsordner konnte nicht als Bibliothekspfad verwendet werden.");
            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", Path.Combine(vlcDir, "plugins"));
            instance = VlcNative.libvlc_new(0, IntPtr.Zero);
            Log("libvlc_new " + instance);
            if (instance == IntPtr.Zero) throw new InvalidOperationException("Der VLC-Videokern konnte nicht gestartet werden.");
            player = VlcNative.libvlc_media_player_new(instance);
            Log("media_player_new " + player);
            if (player == IntPtr.Zero) throw new InvalidOperationException("Der VLC-Player konnte nicht erstellt werden.");
            VlcNative.libvlc_media_player_set_hwnd(player, video.Handle);
            Log("set_hwnd");
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length > 1) {
                List<string> incoming = new List<string>();
                for (int i = 1; i < arguments.Length; i++) if (File.Exists(arguments[i])) incoming.Add(arguments[i]);
                int first = files.Count;
                AddFiles(incoming);
                if (first > 0 && incoming.Count > 0) PlayIndex(first);
            }
            timer.Start();
            Log("timer started");
        } catch (Exception ex) {
            Log("exception " + ex);
            MessageBox.Show(this, ex.Message, "Surface Touch Mediaplayer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        bool start = files.Count == 0;
        foreach (string path in paths) {
            if (PlaylistReader.IsPlaylist(path)) ImportPlaylist(path);
            else AddEntry(path, "");
        }
        if (start && files.Count > 0) PlayIndex(0);
    }

    private bool AddEntry(string source, string displayTitle)
    {
        if (String.IsNullOrWhiteSpace(source)) return false;
        bool local = Path.IsPathRooted(source);
        Uri uri;
        if (local) {
            if (!File.Exists(source)) return false;
            source = Path.GetFullPath(source);
        } else if (!Uri.TryCreate(source, UriKind.Absolute, out uri) || uri.IsFile) {
            if (!File.Exists(source)) return false;
            source = Path.GetFullPath(source);
            local = true;
        }
        string label = displayTitle == null ? "" : displayTitle.Trim();
        if (label.Length == 0) {
            if (local) label = Path.GetFileName(source);
            else {
                Uri.TryCreate(source, UriKind.Absolute, out uri);
                label = Uri.UnescapeDataString(uri.Segments[uri.Segments.Length - 1]).Trim('/');
                if (label.Length == 0) label = uri.Host;
            }
        }
        files.Add(source);
        playlist.Items.Add(label);
        if (shuffleEnabled) shuffleRemaining.Add(files.Count - 1);
        return true;
    }

    private void ImportPlaylist(string path)
    {
        if (!File.Exists(path)) return;
        try {
            List<PlaylistEntry> entries = PlaylistReader.Read(path);
            int added = 0;
            foreach (PlaylistEntry entry in entries) if (AddEntry(entry.Source, entry.Title)) added++;
            if (added == 0) MessageBox.Show(this, "Die Wiedergabeliste enthält keine erreichbaren Medien.", "Surface Touch Mediaplayer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (added < entries.Count) MessageBox.Show(this, added + " Titel übernommen; " + (entries.Count - added) + " nicht erreichbar.", "Surface Touch Mediaplayer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            MessageBox.Show(this, "Die Wiedergabeliste konnte nicht gelesen werden: " + ex.Message, "Surface Touch Mediaplayer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenFiles()
    {
        using (OpenFileDialog dialog = new OpenFileDialog()) {
            dialog.Multiselect = true;
            dialog.Filter = "Medien und Listen|*.mp4;*.mkv;*.avi;*.mov;*.webm;*.mp3;*.flac;*.wav;*.m4a;*.ts;*.wmv;*.mpeg;*.mpg;*.xspf;*.m3u;*.m3u8;*.pls;*.asx;*.wpl;*.vlc;*.ram|Wiedergabelisten|*.xspf;*.m3u;*.m3u8;*.pls;*.asx;*.wpl;*.vlc;*.ram|Alle Dateien|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK) AddFiles(dialog.FileNames);
        }
    }

    private void PlayIndex(int index) { PlayIndex(index, true); }
    private void PlayIndex(int index, bool rememberShuffleHistory)
    {
        if (player == IntPtr.Zero || index < 0 || index >= files.Count) return;
        string source = files[index];
        byte[] bytes = Encoding.UTF8.GetBytes(source + "\0");
        IntPtr path = Marshal.AllocHGlobal(bytes.Length);
        IntPtr media = IntPtr.Zero;
        try {
            Marshal.Copy(bytes, 0, path, bytes.Length);
            Uri uri;
            bool remote = !Path.IsPathRooted(source) && Uri.TryCreate(source, UriKind.Absolute, out uri) && !uri.IsFile;
            media = remote ? VlcNative.libvlc_media_new_location(instance, path) : VlcNative.libvlc_media_new_path(instance, path);
        } finally { Marshal.FreeHGlobal(path); }
        if (media == IntPtr.Zero) {
            MessageBox.Show(this, "Diese Datei konnte nicht geöffnet werden.", "Surface Touch Mediaplayer");
            return;
        }
        VlcNative.libvlc_media_player_set_media(player, media);
        VlcNative.libvlc_media_release(media);
        if (shuffleEnabled) {
            if (rememberShuffleHistory && current >= 0 && current != index) shuffleHistory.Push(current);
            shuffleRemaining.Remove(index);
        }
        current = index;
        endedHandled = false;
        playlist.SelectedIndex = index;
        title.Text = playlist.Items[index].ToString();
        VlcNative.libvlc_media_player_play(player);
    }

    private void PlaySelected() { if (playlist.SelectedIndex >= 0) PlayIndex(playlist.SelectedIndex); }
    private void TogglePlayback()
    {
        if (player == IntPtr.Zero) return;
        if (playlist.SelectedIndex >= 0 && playlist.SelectedIndex != current) { PlayIndex(playlist.SelectedIndex); return; }
        int state = VlcNative.libvlc_media_player_get_state(player);
        if (state == 3) VlcNative.libvlc_media_player_set_pause(player, 1);
        else if (state == 4) VlcNative.libvlc_media_player_set_pause(player, 0);
        else if (current >= 0) PlayIndex(current);
        else if (files.Count > 0) PlayIndex(0);
    }
    private void StopPlayback()
    {
        if (player != IntPtr.Zero) VlcNative.libvlc_media_player_stop(player);
        seek.Fraction = 0;
    }
    private void Next()
    {
        if (files.Count == 0) return;
        if (shuffleEnabled) PlayNextShuffle(true);
        else PlayIndex((current + 1) % files.Count);
    }
    private void Previous()
    {
        if (files.Count == 0) return;
        if (shuffleEnabled) {
            if (shuffleHistory.Count == 0) return;
            int previous = shuffleHistory.Pop();
            if (current >= 0 && !shuffleRemaining.Contains(current)) shuffleRemaining.Add(current);
            PlayIndex(previous, false);
        } else PlayIndex((current - 1 + files.Count) % files.Count);
    }
    private void RemoveSelected()
    {
        int index = playlist.SelectedIndex;
        if (index < 0) return;
        bool wasCurrent = index == current;
        if (wasCurrent) StopPlayback();
        files.RemoveAt(index);
        playlist.Items.RemoveAt(index);
        if (wasCurrent) current = -1;
        if (files.Count == 0) {
            current = -1;
            title.Text = "Datei öffnen oder hierher ziehen";
            ResetShuffleState();
        } else if (wasCurrent) {
            ResetShuffleState();
            PlayIndex(Math.Min(index, files.Count - 1));
        } else {
            if (index < current) current--;
            playlist.SelectedIndex = Math.Min(index, files.Count - 1);
            ResetShuffleState();
        }
    }
    private void ClearPlaylist()
    {
        if (files.Count == 0) return;
        StopPlayback();
        files.Clear();
        playlist.Items.Clear();
        current = -1;
        endedHandled = false;
        title.Text = "Datei öffnen oder hierher ziehen";
        time.Text = "00:00 / 00:00";
        seek.Fraction = 0;
        ResetShuffleState();
    }
    private void ScrollList(int delta)
    {
        if (playlist.Items.Count == 0) return;
        playlist.TopIndex = Math.Max(0, Math.Min(playlist.Items.Count - 1, playlist.TopIndex + delta));
    }
    private void ChangeVolume(int delta)
    {
        if (player == IntPtr.Zero) return;
        int now = Math.Max(0, VlcNative.libvlc_audio_get_volume(player));
        int next = Math.Max(0, Math.Min(200, now + delta));
        VlcNative.libvlc_audio_set_volume(player, next);
        volume.Text = next + "%";
    }
    private void ToggleMute()
    {
        if (player == IntPtr.Zero) return;
        VlcNative.libvlc_audio_toggle_mute(player);
        muted = !muted;
        muteButton.Text = muted ? "🔇" : "🔊";
        SetModeButton(muteButton, muted);
    }
    private void ToggleShuffle()
    {
        shuffleEnabled = !shuffleEnabled;
        ResetShuffleState();
        SetModeButton(shuffleButton, shuffleEnabled);
        tips.SetToolTip(shuffleButton, shuffleEnabled ? "Zufallswiedergabe: an" : "Zufallswiedergabe: aus");
    }
    private void CycleRepeatMode()
    {
        repeatMode = repeatMode == RepeatMode.Off ? RepeatMode.All : repeatMode == RepeatMode.All ? RepeatMode.One : RepeatMode.Off;
        repeatButton.Text = repeatMode == RepeatMode.One ? "🔂" : "🔁";
        SetModeButton(repeatButton, repeatMode != RepeatMode.Off);
        tips.SetToolTip(repeatButton, repeatMode == RepeatMode.All ? "Wiederholen: gesamte Liste" : repeatMode == RepeatMode.One ? "Wiederholen: aktueller Titel" : "Wiederholen: aus");
    }
    private void SetModeButton(Button target, bool active)
    {
        target.BackColor = active ? accent : button;
        target.ForeColor = active ? bg : Color.White;
    }
    private void ResetShuffleState()
    {
        shuffleRemaining.Clear();
        shuffleHistory.Clear();
        if (!shuffleEnabled) return;
        for (int i = 0; i < files.Count; i++) if (i != current) shuffleRemaining.Add(i);
    }
    private bool PlayNextShuffle(bool manual)
    {
        if (files.Count == 0) return false;
        if (files.Count == 1) { PlayIndex(0); return true; }
        if (shuffleRemaining.Count == 0) {
            if (!manual && repeatMode != RepeatMode.All) return false;
            for (int i = 0; i < files.Count; i++) if (i != current) shuffleRemaining.Add(i);
        }
        int position = random.Next(shuffleRemaining.Count);
        int next = shuffleRemaining[position];
        shuffleRemaining.RemoveAt(position);
        PlayIndex(next);
        return true;
    }
    private void ToggleMaximize()
    {
        if (fullscreen) ToggleFullscreen();
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
    }
    private void TogglePlaylist()
    {
        if (fullscreen) return;
        playlistVisible = !playlistVisible;
        ApplyPlaylistVisibility();
    }
    private void ApplyPlaylistVisibility()
    {
        bool show = !fullscreen && playlistVisible;
        middle.SuspendLayout();
        side.Visible = show;
        middle.ColumnStyles[0].Width = show ? 70 : 100;
        middle.ColumnStyles[1].Width = show ? 30 : 0;
        video.Margin = show ? new Padding(0, 0, U(12), 0) : new Padding(0);
        playlistButton.Text = playlistVisible ? "LISTE AUS" : "LISTE AN";
        middle.ResumeLayout(true);
    }
    private void ToggleFullscreen()
    {
        if (!fullscreen) {
            savedState = WindowState;
            if (WindowState == FormWindowState.Normal) savedBounds = Bounds;
            fullscreen = true;
            top.Visible = false;
            root.RowStyles[0].Height = 0;
            side.Visible = false;
            middle.ColumnStyles[0].Width = 100;
            middle.ColumnStyles[1].Width = 0;
            video.Margin = new Padding(0);
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            Bounds = Screen.FromControl(this).Bounds;
            TopMost = true;
            SetModeButton(fullButton, true);
            tips.SetToolTip(fullButton, "Vollbild verlassen");
        } else {
            fullscreen = false;
            TopMost = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            top.Visible = true;
            root.RowStyles[0].Height = U(86);
            ApplyPlaylistVisibility();
            if (savedState == FormWindowState.Maximized) WindowState = FormWindowState.Maximized;
            else { WindowState = FormWindowState.Normal; Bounds = savedBounds; }
            SetModeButton(fullButton, false);
            tips.SetToolTip(fullButton, "Vollbild");
        }
    }
    private void OnTick(object sender, EventArgs e)
    {
        if (player == IntPtr.Zero) return;
        int state = VlcNative.libvlc_media_player_get_state(player);
        if (state != lastState) { lastState = state; Log("state " + state); }
        long length = VlcNative.libvlc_media_player_get_length(player);
        long elapsed = VlcNative.libvlc_media_player_get_time(player);
        if (!seek.IsDragging && length > 0 && elapsed >= 0) seek.Fraction = (double)elapsed / length;
        time.Text = FormatTime(elapsed) + " / " + FormatTime(length);
        playPause.Text = state == 3 ? "Ⅱ" : "▶";
        if (state == 6 && !endedHandled) {
            endedHandled = true;
            if (repeatMode == RepeatMode.One && current >= 0) PlayIndex(current, false);
            else if (shuffleEnabled) PlayNextShuffle(false);
            else if (current + 1 < files.Count) PlayIndex(current + 1);
            else if (repeatMode == RepeatMode.All && files.Count > 0) PlayIndex(0);
        }
    }
    private static string FormatTime(long milliseconds)
    {
        if (milliseconds < 0) milliseconds = 0;
        TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
    }
    private void DrawPlaylistItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using (Brush brush = new SolidBrush(selected ? Color.FromArgb(61, 95, 120) : (e.Index % 2 == 0 ? panel : Color.FromArgb(29, 44, 64))))
            e.Graphics.FillRectangle(brush, e.Bounds);
        Rectangle textRect = new Rectangle(e.Bounds.X + U(12), e.Bounds.Y, e.Bounds.Width - U(18), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, playlist.Items[e.Index].ToString(), playlist.Font, textRect, e.Index == current ? accent : Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    }
    private void OnDragDrop(object sender, DragEventArgs e)
    {
        string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (paths != null) AddFiles(paths);
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11 || (fullscreen && e.KeyCode == Keys.Escape)) { ToggleFullscreen(); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.O) { OpenFiles(); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.L) { TogglePlaylist(); e.Handled = true; }
        else if (e.KeyCode == Keys.Delete && playlist.Focused) { RemoveSelected(); e.Handled = true; }
        else if (e.KeyCode == Keys.Space && !playlist.Focused) { TogglePlayback(); e.Handled = true; }
    }
    private void OnClosing(object sender, FormClosingEventArgs e)
    {
        Log("closing");
        if (resourcesReleased) return;
        if (closingInBackground) { e.Cancel = true; return; }
        closingInBackground = true;
        e.Cancel = true;
        timer.Stop();
        Enabled = false;
        IntPtr playerToRelease = player;
        IntPtr instanceToRelease = instance;
        player = IntPtr.Zero;
        instance = IntPtr.Zero;
        Task.Factory.StartNew(delegate {
            try {
                if (playerToRelease != IntPtr.Zero) { VlcNative.libvlc_media_player_stop(playerToRelease); VlcNative.libvlc_media_player_release(playerToRelease); }
                if (instanceToRelease != IntPtr.Zero) VlcNative.libvlc_release(instanceToRelease);
                VlcNative.SetDllDirectory(null);
                Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", null);
            } finally {
                try { BeginInvoke(new MethodInvoker(delegate { resourcesReleased = true; Close(); })); } catch (InvalidOperationException) { }
            }
        });
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TouchPlayer());
    }
}
