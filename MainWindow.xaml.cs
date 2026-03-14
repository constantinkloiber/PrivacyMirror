using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
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

    public class MonitorEntry
    {
        public WinForms.Screen Screen { get; init; } = WinForms.Screen.PrimaryScreen!;
        public string Label  { get; init; } = "";
        public override string ToString() => Label;
    }

    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lp, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] static extern int  GetWindowText(IntPtr hWnd, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern int  GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr GetShellWindow();
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        const uint SWP_NOSIZE     = 0x0001;
        const uint SWP_NOZORDER   = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;


        // Das permanente Schwarzfenster auf dem Beamer
        BlackWindow?  _blackWindow;
        MirrorWindow? _mirrorWindow;

        public MainWindow()
        {
            InitializeComponent();
            LoadMonitors();
        }

        // ── Monitor laden ────────────────────────────────────────────────────

        void LoadMonitors()
        {
            MonitorComboBox.Items.Clear();
            var screens = WinForms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                var label = $"Monitor {i + 1}  ({s.Bounds.Width}×{s.Bounds.Height})" +
                            (s.Primary ? "  [Haupt]" : "");
                MonitorComboBox.Items.Add(new MonitorEntry { Screen = s, Label = label });
            }
            var secondary = MonitorComboBox.Items.Cast<MonitorEntry>()
                                           .FirstOrDefault(m => !m.Screen.Primary);
            MonitorComboBox.SelectedItem = secondary ?? MonitorComboBox.Items[0];
        }

        // ── Monitor reservieren ──────────────────────────────────────────────

        void ReserveMonitor_Click(object sender, RoutedEventArgs e)
        {
            if (MonitorComboBox.SelectedItem is not MonitorEntry mon) return;

            // Altes Schwarzfenster schließen falls vorhanden
            _blackWindow?.Close();

            _blackWindow = new BlackWindow(mon.Screen);
            _blackWindow.Show();

            // UI in "reserviert"-Zustand
            SetReservedState(mon.Screen);
            LoadWindows();
        }

        void SetReservedState(WinForms.Screen screen)
        {
            MonitorComboBox.IsEnabled = false;
            ReserveButton.IsEnabled   = false;
            WindowListBox.IsEnabled   = true;
            ReleaseButton.IsEnabled   = true;

            StatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(5, 46, 22));
            StatusText.Foreground  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 222, 128));
            StatusText.Text = $"⬛  Monitor {screen.Bounds.Width}×{screen.Bounds.Height} ist reserviert – Beamer zeigt Schwarz";
        }

        // ── Monitor freigeben ────────────────────────────────────────────────

        void ReleaseMonitor_Click(object sender, RoutedEventArgs e) => ReleaseAll();

        void ReleaseAll()
        {
            _mirrorWindow?.Close();
            _mirrorWindow = null;
            _blackWindow?.Close();
            _blackWindow = null;

            // UI zurücksetzen
            MonitorComboBox.IsEnabled   = true;
            ReserveButton.IsEnabled     = true;
            WindowListBox.IsEnabled     = false;
            WindowListBox.SelectedItem  = null;
            MirrorButton.IsEnabled      = false;
            MirrorButton.Content        = "▶  Spiegelung starten";
            ReleaseButton.IsEnabled     = false;

            StatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            StatusText.Foreground  = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            StatusText.Text        = "⚪  Kein Monitor reserviert";
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

        void WindowListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MirrorButton.IsEnabled = WindowListBox.SelectedItem is WindowEntry && _blackWindow != null;
        }

        // ── Spiegelung starten / stoppen ─────────────────────────────────────

        void StartMirror_Click(object sender, RoutedEventArgs e)
        {
            // Läuft bereits → stoppen, Beamer zeigt wieder Schwarz
            if (_mirrorWindow != null)
            {
                _mirrorWindow.Close();
                _mirrorWindow = null;
                MirrorButton.Content   = "▶  Spiegelung starten";
                StatusText.Text = $"⬛  Spiegelung gestoppt – Beamer zeigt Schwarz";
                return;
            }

            if (WindowListBox.SelectedItem is not WindowEntry win) return;
            if (_blackWindow == null) return;

            _mirrorWindow = new MirrorWindow(win.Handle, win.Title, _blackWindow);
            _mirrorWindow.Closed += (_, _) =>
            {
                _mirrorWindow = null;
                MirrorButton.Content = "▶  Spiegelung starten";
            };
            _mirrorWindow.Show();
            MirrorButton.Content = "⏹  Spiegelung stoppen";
            StatusText.Text = $"🔵  Spiegele: {win.Title}";
        }


        // Alle Fenster auf den Hauptmonitor verschieben
        void GatherWindows_Click(object sender, RoutedEventArgs e)
        {
            var primary = WinForms.Screen.PrimaryScreen;
            if (primary == null || _blackWindow == null) return;

            // HWNDs der eigenen Fenster ermitteln - diese niemals verschieben
            var blackHwnd  = new System.Windows.Interop.WindowInteropHelper(_blackWindow).Handle;
            var mirrorHwnd = _mirrorWindow != null
                ? new System.Windows.Interop.WindowInteropHelper(_mirrorWindow).Handle
                : IntPtr.Zero;

            int targetX = primary.WorkingArea.Left + 50;
            int targetY = primary.WorkingArea.Top  + 50;

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                int len = GetWindowTextLength(hWnd);
                if (len == 0) return true;

                // Eigene PrivacyMirror-Fenster nie verschieben
                if (hWnd == blackHwnd || hWnd == mirrorHwnd) return true;

                var screen = WinForms.Screen.FromHandle(hWnd);
                if (screen.Bounds == _blackWindow.Screen.Bounds)
                {
                    SetWindowPos(hWnd, IntPtr.Zero,
                        targetX, targetY, 0, 0,
                        SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                    targetY += 30;
                }
                return true;
            }, IntPtr.Zero);
        }

        void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            ReleaseAll();
        }
    }
}
