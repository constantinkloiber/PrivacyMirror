using System.Windows;
using WinForms = System.Windows.Forms;

namespace PrivacyMirror
{
    public partial class BlackWindow : Window
    {
        public WinForms.Screen Screen { get; }

        public BlackWindow(WinForms.Screen screen)
        {
            InitializeComponent();
            Screen = screen;

            var b  = screen.Bounds;
            Left   = b.Left;
            Top    = b.Top;
            Width  = b.Width;
            Height = b.Height;

            Loaded += (_, _) => WindowState = WindowState.Maximized;
        }
    }
}
