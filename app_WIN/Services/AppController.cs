using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Interop;
using MeetingBar.Windows.Interop;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.ViewModels;
using MeetingBar.Windows.Views;
using Forms = System.Windows.Forms;

namespace MeetingBar.Windows.Services
{
    public sealed class AppController : IDisposable
    {
        private readonly SettingsService _settings = new SettingsService();
        private readonly CalendarSyncService _sync;
        private readonly MeetingOpener _opener;
        private readonly Forms.NotifyIcon _tray;
        private readonly Icon _icon;
        private readonly HwndSource _messageWindow;
        private readonly MainViewModel _main;
        private readonly NotificationService _notifications;
        private readonly DispatcherTimer _positionTimer;
        private PopupWindow _popup;
        private TaskbarWidgetWindow _widget;
        private readonly CancellationTokenSource _ipcCancellation = new CancellationTokenSource();

        public AppController()
        {
            _sync = new CalendarSyncService(_settings); _opener = new MeetingOpener(_settings); _main = new MainViewModel(_sync, _settings, _opener);
            _icon = CreateTrayIcon();
            _tray = new Forms.NotifyIcon { Icon = _icon, Visible = true, Text = "MeetingBar" };
            _tray.MouseClick += (s, e) => { if (e.Button == Forms.MouseButtons.Left) TogglePopup(); };
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open MeetingBar", null, (s, e) => ShowPopup());
            menu.Items.Add("Refresh", null, async (s, e) => await _sync.RefreshAsync());
            menu.Items.Add("Settings", null, (s, e) => OpenSettings());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Quit", null, (s, e) => Application.Current.Shutdown());
            _tray.ContextMenuStrip = menu;
            _notifications = new NotificationService(_settings, _opener, _tray);
            _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) }; _positionTimer.Tick += (s, e) => EnsureWidgetVisible();
            _sync.Changed += (s, e) => Application.Current.Dispatcher.BeginInvoke(new Action(UpdateTray));
            var parameters = new HwndSourceParameters("MeetingBar.MessageWindow") { Width = 0, Height = 0, WindowStyle = unchecked((int)0x80000000) };
            _messageWindow = new HwndSource(parameters); _messageWindow.AddHook(WndProc);
        }

        public void Start()
        {
            ApplySettings(); _sync.Start(); _notifications.Start(() => _sync.Events); _positionTimer.Start(); UpdateTray(); _ = ListenForCommands();
        }

        private async Task ListenForCommands()
        {
            while (!_ipcCancellation.IsCancellationRequested)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream("MeetingBar.Windows.Commands", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await pipe.WaitForConnectionAsync(_ipcCancellation.Token);
                        using (var reader = new StreamReader(pipe))
                        {
                            var command = await reader.ReadLineAsync();
                            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() => ExecuteCommand(command)));
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { if (!_ipcCancellation.IsCancellationRequested) await Task.Delay(250); }
            }
        }

        public void ExecuteCommand(string command)
        {
            switch ((command ?? "popup").Trim().TrimStart('-').ToLowerInvariant())
            {
                case "join": _opener.Open(_main.NextEvent); break;
                case "create": _opener.Open(_settings.Current.CreateMeetingUrl); break;
                case "refresh": _ = _sync.RefreshAsync(); break;
                case "settings": OpenSettings(); break;
                case "quit": Application.Current.Shutdown(); break;
                default: ShowPopup(); break;
            }
        }

        private void ApplySettings()
        {
            if (_settings.Current.WidgetMode == WidgetMode.TrayOnly)
            {
                _widget?.Hide();
            }
            else
            {
                if (_widget == null) CreateWidget();
                _widget.ApplyPosition(_settings.Current); _widget.Show();
            }
            NativeMethods.UnregisterHotKey(_messageWindow.Handle, 1);
            if (_settings.Current.GlobalJoinHotkey) NativeMethods.RegisterHotKey(_messageWindow.Handle, 1, NativeMethods.ModControl | NativeMethods.ModAlt, (uint)Forms.Keys.J);
            _sync.Schedule(); UpdateTray();
        }

        private void CreateWidget()
        {
            _widget = new TaskbarWidgetWindow(_main, TogglePopup);
            _widget.Closed += (s, e) => _widget = null;
        }

        private void EnsureWidgetVisible()
        {
            if (_settings.Current.WidgetMode == WidgetMode.TrayOnly) return;
            if (_widget == null) CreateWidget();
            if (!_widget.IsVisible) _widget.Show();
            var widgetHandle = new WindowInteropHelper(_widget).Handle;
            if (widgetHandle != IntPtr.Zero && !NativeMethods.IsWindowVisible(widgetHandle)) NativeMethods.ShowWindow(widgetHandle, 5);
            _widget.ApplyPosition(_settings.Current);
            if (widgetHandle != IntPtr.Zero) NativeMethods.SetWindowPos(widgetHandle, NativeMethods.HwndTopmost, 0, 0, 0, 0, NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }

        private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == NativeMethods.WmHotKey && wParam.ToInt32() == 1) { _opener.Open(_main.NextEvent); handled = true; }
            return IntPtr.Zero;
        }

        private void TogglePopup() { if (_popup?.IsVisible == true) _popup.Hide(); else ShowPopup(); }
        private void ShowPopup()
        {
            if (_popup == null) _popup = new PopupWindow(_main, OpenSettings);
            PositionPopup(_popup); _popup.Show(); _popup.Activate();
        }

        private void PositionPopup(Window popup)
        {
            popup.UpdateLayout();
            var work = SystemParameters.WorkArea;
            if (_widget?.IsVisible == true)
            {
                popup.Left = Math.Max(work.Left + 8, Math.Min(work.Right - popup.Width - 8, _widget.Left + _widget.Width - popup.Width));
                popup.Top = Math.Max(work.Top + 8, _widget.Top - popup.Height - 8);
            }
            else { popup.Left = work.Right - popup.Width - 10; popup.Top = work.Bottom - popup.Height - 10; }
        }

        private void OpenSettings()
        {
            var existing = Application.Current.Windows;
            foreach (Window window in existing) if (window is SettingsWindow) { window.Activate(); return; }
            var vm = new SettingsViewModel(_settings, _sync, async () => { ApplySettings(); await _sync.RefreshAsync(); });
            new SettingsWindow(vm).Show();
        }

        private void UpdateTray()
        {
            var item = _main.NextEvent;
            var text = item == null ? "MeetingBar — no upcoming meetings" : item.Title + " — " + item.Countdown;
            _tray.Text = text.Length > 63 ? text.Substring(0, 63) : text;
            _widget?.ApplyPosition(_settings.Current);
        }

        private static Icon CreateTrayIcon()
        {
            try
            {
                var extracted = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                if (extracted != null) return (Icon)extracted.Clone();
            }
            catch { }
            var bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            using (var background = new SolidBrush(System.Drawing.Color.FromArgb(255, 109, 124, 255)))
            using (var white = new SolidBrush(System.Drawing.Color.White))
            using (var pen = new Pen(System.Drawing.Color.White, 2))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.FillRoundedRectangle(background, 2, 3, 28, 27, 6);
                g.DrawLine(pen, 8, 1, 8, 8); g.DrawLine(pen, 24, 1, 24, 8); g.DrawLine(pen, 3, 11, 29, 11);
                g.FillRectangle(white, 8, 16, 5, 5); g.FillRectangle(white, 18, 16, 5, 5);
            }
            var handle = bitmap.GetHicon();
            try { using (var borrowed = Icon.FromHandle(handle)) return (Icon)borrowed.Clone(); }
            finally { NativeMethods.DestroyIcon(handle); bitmap.Dispose(); }
        }

        public void Dispose()
        {
            _ipcCancellation.Cancel(); _positionTimer.Stop(); NativeMethods.UnregisterHotKey(_messageWindow.Handle, 1); _messageWindow.Dispose(); _notifications.Dispose(); _main.Dispose(); _sync.Dispose(); _tray.Visible = false; _tray.Dispose(); _icon.Dispose(); _ipcCancellation.Dispose();
        }
    }

    internal static class DrawingExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, float x, float y, float width, float height, float radius)
        {
            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                var d = radius * 2; path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90); path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90); path.CloseFigure(); graphics.FillPath(brush, path);
            }
        }
    }
}
