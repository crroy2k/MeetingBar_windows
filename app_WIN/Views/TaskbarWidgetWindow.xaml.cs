using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MeetingBar.Windows.Interop;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.ViewModels;

namespace MeetingBar.Windows.Views
{
    public partial class TaskbarWidgetWindow : Window
    {
        private readonly Action _clicked;
        public TaskbarWidgetWindow(MainViewModel vm, Action clicked) { InitializeComponent(); DataContext = vm; _clicked = clicked; SourceInitialized += (s, e) => { var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle); source?.AddHook(WndProc); }; }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) { return IntPtr.Zero; }
        private void Widget_Click(object sender, RoutedEventArgs e) => _clicked();
        public void ApplyPosition(AppSettings settings)
        {
            Width = Math.Max(160, Math.Min(600, settings.WidgetWidth)); Height = 40;
            var taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            NativeMethods.RECT rect;
            if (!NativeMethods.GetWindowRect(taskbar, out rect)) { Left = SystemParameters.WorkArea.Right - Width; Top = SystemParameters.WorkArea.Bottom - Height; return; }
            var dpi = VisualTreeHelper.GetDpi(this); var sx = dpi.DpiScaleX; var sy = dpi.DpiScaleY;
            var targetTop = rect.Top / sy + Math.Max(0, ((rect.Bottom - rect.Top) / sy - Height) / 2.0) + settings.WidgetOffsetY;
            Top = Clamp(targetTop, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);
            if (settings.WidgetMode == WidgetMode.TaskbarLeft) Left = Clamp(rect.Left / sx + 8 + settings.WidgetOffsetX, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            else
            {
                var notify = NativeMethods.FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
                NativeMethods.RECT trayRect;
                var targetLeft = NativeMethods.GetWindowRect(notify, out trayRect) ? trayRect.Left / sx - Width - 6 + settings.WidgetOffsetX : rect.Right / sx - Width - 180 + settings.WidgetOffsetX;
                Left = Clamp(targetLeft, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            }
        }
        private static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));
    }
}
