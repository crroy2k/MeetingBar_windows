using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using MeetingBar.Windows.Interop;
using MeetingBar.Windows.ViewModels;
using Microsoft.Win32;

namespace MeetingBar.Windows.Views
{
    public partial class SettingsWindow : Window
    {
        private IntPtr _handle;

        public SettingsWindow(SettingsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            ApplyWindowsTheme();
            SourceInitialized += SettingsWindow_SourceInitialized;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            Closed += (s, e) => SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        }

        private void SettingsWindow_SourceInitialized(object sender, EventArgs e)
        {
            _handle = new WindowInteropHelper(this).Handle;
            var source = HwndSource.FromHwnd(_handle);
            if (source?.CompositionTarget != null) source.CompositionTarget.BackgroundColor = Colors.Transparent;
            ApplyWindowsTheme();
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(ApplyWindowsTheme));

        private void ApplyWindowsTheme()
        {
            var light = AppsUseLightTheme();
            var accent = WindowsAccent();
            if (SystemParameters.HighContrast)
            {
                SetBrush("SettingsBackgroundBrush", SystemColors.WindowColor);
                SetBrush("SettingsCardBrush", SystemColors.ControlColor);
                SetBrush("SettingsInputBrush", SystemColors.WindowColor);
                SetBrush("SettingsTextBrush", SystemColors.WindowTextColor);
                SetBrush("SettingsMutedBrush", SystemColors.GrayTextColor);
                SetBrush("SettingsBorderBrush", SystemColors.WindowTextColor);
                SetBrush("SettingsHoverBrush", SystemColors.HighlightColor);
                SetBrush("SettingsSelectedBrush", SystemColors.HighlightColor);
            }
            else if (light)
            {
                SetBrush("SettingsBackgroundBrush", Color.FromArgb(232, 243, 243, 243));
                SetBrush("SettingsCardBrush", Color.FromRgb(255, 255, 255));
                SetBrush("SettingsInputBrush", Color.FromRgb(255, 255, 255));
                SetBrush("SettingsTextBrush", Color.FromRgb(26, 26, 26));
                SetBrush("SettingsMutedBrush", Color.FromRgb(97, 97, 97));
                SetBrush("SettingsBorderBrush", Color.FromArgb(36, 0, 0, 0));
                SetBrush("SettingsHoverBrush", Color.FromArgb(16, 0, 0, 0));
                SetBrush("SettingsSelectedBrush", Color.FromArgb(22, 0, 0, 0));
            }
            else
            {
                SetBrush("SettingsBackgroundBrush", Color.FromArgb(232, 32, 32, 32));
                SetBrush("SettingsCardBrush", Color.FromRgb(45, 45, 45));
                SetBrush("SettingsInputBrush", Color.FromRgb(36, 36, 36));
                SetBrush("SettingsTextBrush", Color.FromRgb(248, 248, 248));
                SetBrush("SettingsMutedBrush", Color.FromRgb(184, 184, 184));
                SetBrush("SettingsBorderBrush", Color.FromArgb(42, 255, 255, 255));
                SetBrush("SettingsHoverBrush", Color.FromArgb(22, 255, 255, 255));
                SetBrush("SettingsSelectedBrush", Color.FromArgb(30, 255, 255, 255));
            }
            SetBrush("AccentBrush", accent);
            SetBrush("InkBrush", BrushColor("SettingsTextBrush"));
            SetBrush("MutedBrush", BrushColor("SettingsMutedBrush"));
            SetBrush("PanelBrush", BrushColor("SettingsBackgroundBrush"));
            SetBrush("PanelRaisedBrush", BrushColor("SettingsCardBrush"));
            Foreground = FindResource("SettingsTextBrush") as Brush;
            if (_handle != IntPtr.Zero) NativeMethods.EnableMainWindowBackdrop(_handle, !light);
        }

        private void SetBrush(string key, Color color)
        {
            var brush = Resources[key] as SolidColorBrush;
            if (brush != null && !brush.IsFrozen) brush.Color = color;
            else Resources[key] = new SolidColorBrush(color);
        }

        private Color BrushColor(string key) => (Resources[key] as SolidColorBrush)?.Color ?? Colors.Transparent;

        private static bool AppsUseLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) == 1;
            }
            catch { return true; }
        }

        private static Color WindowsAccent()
        {
            try
            {
                uint value; bool opaque;
                if (NativeMethods.DwmGetColorizationColor(out value, out opaque) == 0)
                    return Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
            }
            catch { }
            return Color.FromRgb(0, 120, 212);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
        private void OpenLink_Click(object sender, RequestNavigateEventArgs e) { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); e.Handled = true; }
    }
}
