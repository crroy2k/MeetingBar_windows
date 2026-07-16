using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using MeetingBar.Windows.Interop;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class PopupWindow : Window
    {
        private readonly Action _openSettings;
        private IntPtr _handle;
        private bool _isClosing;
        private bool _isAnimating;
        private bool _isPreloaded;
        private bool _isPreloading;
        private bool _isOpen;
        private double _restingTop;

        public bool IsOpen => _isOpen;
        public bool WasJustDismissed => DateTime.UtcNow - LastHiddenAtUtc < TimeSpan.FromMilliseconds(350);
        public DateTime LastHiddenAtUtc { get; private set; } = DateTime.MinValue;

        public PopupWindow(MainViewModel viewModel, Action openSettings)
        {
            App.ApplyWindowsAccent();
            InitializeComponent();
            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 0,
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                UseAeroCaptionButtons = false
            });
            DataContext = viewModel;
            _openSettings = openSettings;
            SourceInitialized += Window_SourceInitialized;
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) HideAnimated(); };
        }

        public void Preload()
        {
            if (_isPreloaded) return;
            _isPreloading = true;
            ShowActivated = false;
            Left = SystemParameters.VirtualScreenLeft - Width - 100;
            Top = SystemParameters.VirtualScreenTop;
            Show();
            UpdateLayout();
            Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
            NativeMethods.ShowWindow(_handle, 0);
            ShowActivated = true;
            _isPreloaded = true;
            _isPreloading = false;
        }

        public async void ShowAnimated()
        {
            App.ApplyWindowsAccent();
            if (_isOpen) { Activate(); return; }
            if (!_isPreloaded) Preload();
            _isClosing = false;
            _isAnimating = true;
            ApplyBackdrop();
            _isOpen = true;
            _restingTop = Top;
            NativeMethods.RECT rect;
            if (!NativeMethods.GetWindowRect(_handle, out rect)) { NativeMethods.ShowWindow(_handle, 5); _isAnimating = false; Activate(); return; }
            var targetTop = rect.Top;
            var startTop = rect.Bottom + (int)Math.Round(12 * VisualTreeHelper.GetDpi(this).DpiScaleY);
            MoveWindow(rect.Left, startTop);
            NativeMethods.ShowWindow(_handle, 5);
            Activate();
            await AnimateWindowAsync(rect.Left, startTop, targetTop, false);
            Top = _restingTop;
            _isAnimating = false;
        }

        public async void HideAnimated()
        {
            if (!_isOpen || _isClosing || _isAnimating) return;
            _isClosing = true;
            _isAnimating = true;
            NativeMethods.RECT rect;
            if (NativeMethods.GetWindowRect(_handle, out rect))
            {
                var startTop = rect.Top;
                var endTop = rect.Bottom + (int)Math.Round(12 * VisualTreeHelper.GetDpi(this).DpiScaleY);
                await AnimateWindowAsync(rect.Left, startTop, endTop, true);
                NativeMethods.ShowWindow(_handle, 0);
                MoveWindow(rect.Left, startTop);
            }
            else NativeMethods.ShowWindow(_handle, 0);
            Top = _restingTop;
            _isOpen = false;
            LastHiddenAtUtc = DateTime.UtcNow;
            _isAnimating = false;
            _isClosing = false;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            _handle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(_handle);
            if (source?.CompositionTarget != null) source.CompositionTarget.BackgroundColor = Colors.Transparent;
            source?.AddHook(WndProc);
            ApplyBackdrop();
        }

        private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0006 || message == 0x031A || message == 0x031E)
                Dispatcher.BeginInvoke(new Action(ApplyBackdrop));
            return IntPtr.Zero;
        }

        private void ApplyBackdrop()
        {
            if (_handle == IntPtr.Zero) return;
            var glass = NativeMethods.EnableTransientBackdrop(_handle, true);
            PopupSurface.Background = glass ? new SolidColorBrush(Color.FromArgb(68, 32, 32, 32)) : new SolidColorBrush(Color.FromRgb(32, 32, 32));
        }

        private Task AnimateWindowAsync(int left, int from, int to, bool easeIn)
        {
            return Task.Run(() =>
            {
                var clock = Stopwatch.StartNew();
                const double duration = 167.0;
                while (clock.Elapsed.TotalMilliseconds < duration)
                {
                    var progress = Math.Max(0, Math.Min(1, clock.Elapsed.TotalMilliseconds / duration));
                    var eased = easeIn ? progress * progress : 1 - (1 - progress) * (1 - progress);
                    MoveWindow(left, (int)Math.Round(from + (to - from) * eased));
                    NativeMethods.DwmFlush();
                }
                MoveWindow(left, to);
                NativeMethods.DwmFlush();
            });
        }

        private void MoveWindow(int left, int top) => NativeMethods.SetWindowPos(_handle, IntPtr.Zero, left, top, 0, 0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate | NativeMethods.SwpNoSendChanging);

        private void Window_Deactivated(object sender, EventArgs e) { if (_isOpen && !_isAnimating && !_isPreloading) HideAnimated(); }
        private void Settings_Click(object sender, RoutedEventArgs e) { HideAnimated(); _openSettings(); }
    }
}
