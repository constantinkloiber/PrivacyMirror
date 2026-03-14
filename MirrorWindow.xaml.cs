using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PrivacyMirror
{
    public partial class MirrorWindow : Window
    {
        [DllImport("dwmapi.dll")] static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);
        [DllImport("dwmapi.dll")] static extern int DwmUnregisterThumbnail(IntPtr thumb);
        [DllImport("dwmapi.dll")] static extern int DwmUpdateThumbnailProperties(IntPtr thumb, ref DWM_THUMBNAIL_PROPERTIES props);
        [DllImport("dwmapi.dll")] static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out PSIZE size);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)] struct PSIZE { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] struct RECT  { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct DWM_THUMBNAIL_PROPERTIES
        {
            public uint dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        const uint DWM_TNP_RECTDESTINATION = 0x00000001;
        const uint DWM_TNP_RECTSOURCE      = 0x00000002;
        const uint DWM_TNP_VISIBLE         = 0x00000008;
        const uint DWM_TNP_OPACITY         = 0x00000004;

        readonly IntPtr          _targetHwnd;
        readonly BlackWindow     _blackWindow;
        IntPtr                   _thumbnail = IntPtr.Zero;
        readonly DispatcherTimer _updateTimer;
        DispatcherTimer?         _hintTimer;

        public MirrorWindow(IntPtr targetHwnd, string targetTitle, BlackWindow blackWindow)
        {
            InitializeComponent();
            _targetHwnd  = targetHwnd;
            _blackWindow = blackWindow;
            Title = $"Spiegelung  -  {targetTitle}";

            var b = blackWindow.Screen.Bounds;
            Left   = b.Left;
            Top    = b.Top;
            Width  = b.Width;
            Height = b.Height;

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _updateTimer.Tick += (_, _) => UpdateThumbnail();

            Topmost     = true;
            Loaded      += OnLoaded;
            Closing     += OnClosing;
            SizeChanged += (_, _) => UpdateThumbnail();
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;

            var myHwnd = new WindowInteropHelper(this).Handle;
            int hr = DwmRegisterThumbnail(myHwnd, _targetHwnd, out _thumbnail);

            if (hr != 0 || _thumbnail == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show(
                    $"DWM-Fehler: 0x{hr:X8}\nBitte Windows 10/11 verwenden.",
                    "PrivacyMirror", MessageBoxButton.OK, MessageBoxImage.Error);
                Close(); return;
            }

            UpdateThumbnail();
            _updateTimer.Start();

            _hintTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _hintTimer.Tick += (_, _) => { HintBorder.Visibility = Visibility.Collapsed; _hintTimer.Stop(); };
            _hintTimer.Start();
        }

        void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _updateTimer.Stop();
            if (_thumbnail != IntPtr.Zero) DwmUnregisterThumbnail(_thumbnail);
        }

        void UpdateThumbnail()
        {
            if (_thumbnail == IntPtr.Zero || !IsWindow(_targetHwnd)) return;

            DwmQueryThumbnailSourceSize(_thumbnail, out PSIZE src);
            if (src.x <= 0 || src.y <= 0) return;

            // rcDestination erwartet physische Pixel des Zielfensters.
            // Screen.Bounds liefert diese direkt (unabhaengig von DPI-Skalierung).
            var bounds = _blackWindow.Screen.Bounds;
            double destW = bounds.Width;
            double destH = bounds.Height;

            // Seitenverhältnis halten, zentriert einpassen
            double scale = Math.Min(destW / src.x, destH / src.y);
            int dstW = (int)(src.x * scale);
            int dstH = (int)(src.y * scale);
            int dstX = (int)((destW - dstW) / 2);
            int dstY = (int)((destH - dstH) / 2);

            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags       = DWM_TNP_RECTDESTINATION | DWM_TNP_RECTSOURCE | DWM_TNP_VISIBLE | DWM_TNP_OPACITY,
                rcDestination = new RECT { Left = dstX, Top = dstY, Right = dstX + dstW, Bottom = dstY + dstH },
                rcSource      = new RECT { Left = 0, Top = 0, Right = src.x, Bottom = src.y },
                opacity       = 255,
                fVisible      = true,
                fSourceClientAreaOnly = false
            };
            DwmUpdateThumbnailProperties(_thumbnail, ref props);
        }

        void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape) Close();
        }
    }
}
