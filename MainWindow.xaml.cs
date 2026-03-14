using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using WinForms = System.Windows.Forms;

namespace PrivacyMirror
{
    public class WindowEntry
    {
        public IntPtr Handle { get; init; }
        public string Title  { get; init; } = "";
        public override string ToString() => Title;
    }

    public class MonitorCheckEntry : INotifyPropertyChanged
    {
        public WinForms.Screen Screen { get; init; } = WinForms.Screen.PrimaryScreen!;
        public string Label { get; init; } = "";

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lp, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] static extern int  GetWindowText(IntPtr hWnd, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern int  GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        const uint SWP_NOSIZE     = 0x0001;
        const uint SWP_NOZORDER   = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;

        readonly Dictionary<string, BlackWindow>  _blackWindows  = new();
        readonly Dictionary<string, MirrorWindow> _mirrorWindows = new();
        List<MonitorCheckEntry> _monitorEntries = new();

        // Wird zur Laufzeit per FindName aus dem XAML-Baum geholt.
        // So compiliert der CS unabhängig davon, welche XAML-Version vorliegt.
        ItemsControl? _monitorItems;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            _monitorItems = FindName("MonitorList") as ItemsControl;

            if (_monitorItems == null)
            {
                System.Windows.MessageBox.Show(
                    "MainWindow.xaml wurde noch nicht aktualisiert.\n\n" +
                    "Bitte ersetze MainWindow.xaml durch die neue Version und baue das Projekt neu.",
                    "PrivacyMirror – Setup erforderlich",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadMonitors();
        }

        // ── Monitor-Liste ────────────────────────────────────────────────────

        void LoadMonitors()
        {
            if (_monitorItems == null) return;

            var screens = WinForms.Screen.AllScreens;

            var previouslyChecked = _monitorEntries
                .Where(e => e.IsChecked)
                .Select(e => e.Screen.DeviceName)
                .ToHashSet();

            _monitorEntries = new List<MonitorCheckEntry>();

            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                _monitorEntries.Add(new MonitorCheckEntry
                {
                    Screen    = s,
                    Label     = $"Monitor {i + 1}   {s.Bounds.Width}×{s.Bounds.Height}" +
                                (s.Primary ? "   [Haupt]" : ""),
                    IsChecked = previouslyChecked.Contains(s.DeviceName)
                });
            }

            var current = screens.Select(s => s.DeviceName).ToHashSet();
            foreach (var key in _blackWindows.Keys.Where(k => !current.Contains(k)).ToList())
                CloseScreenWindows(key);

            _monitorItems.ItemsSource = null;
            _monitorItems.ItemsSource = _monitorEntries;

            UpdateUI();
        }

        void RefreshMonitors_Click(object sender, RoutedEventArgs e) => LoadMonitors();

        void MonitorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox cb &&
                cb.DataContext is MonitorCheckEntry entry)
                ToggleMonitor(entry);
        }

        void ToggleMonitor(MonitorCheckEntry entry)
        {
            string key = entry.Screen.DeviceName;

            if (entry.IsChecked)
            {
                var bw = new BlackWindow(entry.Screen);
                bw.Show();
                _blackWindows[key] = bw;

                if (_mirrorWindows.Count > 0 && WindowListBox.SelectedItem is WindowEntry win)
                {
                    string k = key;
                    var mw = new MirrorWindow(win.Handle, win.Title, bw);
                    mw.Closed += (_, _) => { _mirrorWindows.Remove(k); UpdateUI(); };
                    mw.Show();
                    _mirrorWindows[key] = mw;
                }
            }
            else
            {
                CloseScreenWindows(key);
            }

            UpdateUI();
        }

        void CloseScreenWindows(string key)
        {
            if (_mirrorWindows.TryGetValue(key, out var mw)) { mw.Close(); _mirrorWindows.Remove(key); }
            if (_blackWindows.TryGetValue(key, out var bw))  { bw.Close();  _blackWindows.Remove(key); }

            var entry = _monitorEntries.FirstOrDefault(e => e.Screen.DeviceName == key);
            if (entry != null) entry.IsChecked = false;
        }

        // ── Freigabe ─────────────────────────────────────────────────────────

        void ReleaseMonitor_Click(object sender, RoutedEventArgs e) => ReleaseAll();

        void ReleaseAll()
        {
            foreach (var mw in _mirrorWindows.Values.ToList()) mw.Close();
            _mirrorWindows.Clear();
            foreach (var bw in _blackWindows.Values.ToList()) bw.Close();
            _blackWindows.Clear();
            foreach (var entry in _monitorEntries) entry.IsChecked = false;

            WindowListBox.SelectedItem = null;
            MirrorButton.Content = "▶  Spiegelung starten";
            UpdateUI();
        }

        // ── UI-Zustand ───────────────────────────────────────────────────────

        void UpdateUI()
        {
            bool anyBlacked   = _blackWindows.Count > 0;
            bool anyMirroring = _mirrorWindows.Count > 0;
            bool winSelected  = WindowListBox.SelectedItem is WindowEntry;

            WindowListBox.IsEnabled = anyBlacked;
            MirrorButton.IsEnabled  = anyBlacked && winSelected;
            ReleaseButton.IsEnabled = anyBlacked;
            GatherButton.IsEnabled  = anyBlacked;

            static System.Windows.Media.SolidColorBrush Brush(byte r, byte g, byte b) =>
                new(System.Windows.Media.Color.FromRgb(r, g, b));

            if (anyMirroring && WindowListBox.SelectedItem is WindowEntry win)
            {
                int n = _mirrorWindows.Count;
                StatusBadge.Background = Brush(30, 58, 138);
                StatusText.Foreground  = Brush(147, 197, 253);
                StatusText.Text = $"🔵  Spiegele auf {n} Monitor{(n > 1 ? "en" : "")}: {win.Title}";
            }
            else if (anyBlacked)
            {
                int n = _blackWindows.Count;
                StatusBadge.Background = Brush(5, 46, 22);
                StatusText.Foreground  = Brush(74, 222, 128);
                StatusText.Text = $"⬛  {n} Monitor{(n > 1 ? "e" : "")} reserviert – Beamer zeigt Schwarz";
            }
            else
            {
                StatusBadge.Background = Brush(30, 41, 59);
                StatusText.Foreground  = Brush(100, 116, 139);
                StatusText.Text = "⚪  Kein Monitor reserviert";
            }
        }

        // ── Fensterliste ─────────────────────────────────────────────────────

        void LoadWindows()
        {
            WindowListBox.Items.Clear();
            var shell = GetShellWindow();

            EnumWindows((hWnd, _) =>
            {
                if (hWnd == shell || !IsWindowVisible(hWnd)) return true;
                int len = GetWindowTextLength(hWnd);
                if (len == 0) return true;
                var sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                var title = sb.ToString().Trim();
                if (string.IsNullOrWhiteSpace(title)) return true;
                if (title == Title || title == "PrivacyMirror – Spiegelung") return true;
                WindowListBox.Items.Add(new WindowEntry { Handle = hWnd, Title = title });
                return true;
            }, IntPtr.Zero);
        }

        void RefreshWindows_Click(object sender, RoutedEventArgs e) => LoadWindows();

        void WindowListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateUI();

        // ── Spiegelung ───────────────────────────────────────────────────────

        void StartMirror_Click(object sender, RoutedEventArgs e)
        {
            if (_mirrorWindows.Count > 0)
            {
                foreach (var mw in _mirrorWindows.Values.ToList()) mw.Close();
                _mirrorWindows.Clear();
                MirrorButton.Content = "▶  Spiegelung starten";
                UpdateUI();
                return;
            }

            if (WindowListBox.SelectedItem is not WindowEntry win) return;

            foreach (var kvp in _blackWindows)
            {
                string key = kvp.Key;
                var mw = new MirrorWindow(win.Handle, win.Title, kvp.Value);
                mw.Closed += (_, _) =>
                {
                    _mirrorWindows.Remove(key);
                    if (_mirrorWindows.Count == 0)
                        MirrorButton.Content = "▶  Spiegelung starten";
                    UpdateUI();
                };
                mw.Show();
                _mirrorWindows[key] = mw;
            }

            MirrorButton.Content = "⏹  Spiegelung stoppen";
            UpdateUI();
        }

        // ── Fenster einsammeln ───────────────────────────────────────────────

        void GatherWindows_Click(object sender, RoutedEventArgs e)
        {
            var primary = WinForms.Screen.PrimaryScreen;
            if (primary == null || _blackWindows.Count == 0) return;

            var ownHandles = new HashSet<IntPtr>();
            foreach (var bw in _blackWindows.Values)
                ownHandles.Add(new System.Windows.Interop.WindowInteropHelper(bw).Handle);
            foreach (var mw in _mirrorWindows.Values)
                ownHandles.Add(new System.Windows.Interop.WindowInteropHelper(mw).Handle);

            var blackedBounds = _blackWindows.Values.Select(bw => bw.Screen.Bounds).ToList();

            int targetX = primary.WorkingArea.Left + 50;
            int targetY = primary.WorkingArea.Top  + 50;

            EnumWindows((hWnd, _) =>
            {
                if (ownHandles.Contains(hWnd) || !IsWindowVisible(hWnd)) return true;
                if (GetWindowTextLength(hWnd) == 0) return true;
                var screen = WinForms.Screen.FromHandle(hWnd);
                if (blackedBounds.Any(b => b == screen.Bounds))
                {
                    SetWindowPos(hWnd, IntPtr.Zero, targetX, targetY, 0, 0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                    targetY += 30;
                }
                return true;
            }, IntPtr.Zero);
        }

        void MainWindow_Closing(object? sender, CancelEventArgs e) => ReleaseAll();
    }
}
