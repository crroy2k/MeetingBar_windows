using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using System.Windows.Media;
using MeetingBar.Windows.Interop;
using MeetingBar.Windows.Services;

namespace MeetingBar.Windows
{
    public partial class App : Application
    {
        private Mutex _singleInstance;
        private AppController _controller;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) => AppLog.Fatal("Unhandled", args.ExceptionObject as Exception ?? new Exception("Unknown fatal error"));
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) => { AppLog.Error("Task", args.Exception); args.SetObserved(); };
            DispatcherUnhandledException += (s, args) =>
            {
                AppLog.Error("Dispatcher", args.Exception);
                MessageBox.Show(args.Exception.Message, "MeetingBar", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.ProcessExit += (s, args) => AppLog.ProcessExit(Environment.ExitCode);
            try { Microsoft.Win32.SystemEvents.PowerModeChanged += PowerModeChanged; }
            catch (Exception error) { AppLog.Error("PowerEventsSubscription", error); }
            bool created;
            _singleInstance = new Mutex(true, "Local\\MeetingBar.Windows", out created);
            if (!created) { SendCommand(e.Args.Length == 0 ? "popup" : e.Args[0].TrimStart('-')); Shutdown(); return; }
            AppLog.BeginSession();
            MeetingBar.Localization.SpanishText.Initialize();
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage("es-CR")));
            // This small, mostly static overlay does not need a resident GPU rendering context.
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            base.OnStartup(e);
            ApplyWindowsAccent();
            _controller = new AppController();
            AppLog.Info("Application started");
            _controller.Start();
            if (e.Args.Length > 0) _controller.ExecuteCommand(e.Args[0].TrimStart('-'), "startup");
        }

        internal static void ApplyWindowsAccent()
        {
            try
            {
                uint value; bool opaque;
                if (NativeMethods.DwmGetColorizationColor(out value, out opaque) != 0) return;
                var color = Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
                Current.Resources["Accent"] = color;
                Current.Resources["AccentBrush"] = new SolidColorBrush(color);
            }
            catch { }
        }

        private static void SendCommand(string command)
        {
            try
            {
                using (var pipe = new NamedPipeClientStream(".", "MeetingBar.Windows.Commands", PipeDirection.Out))
                using (var writer = new StreamWriter(pipe))
                { pipe.Connect(1500); writer.WriteLine(command); writer.Flush(); }
            }
            catch { }
        }

        private static void PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
            => AppLog.Info("Power state changed; mode=" + e.Mode);

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            AppLog.RequestExit(e.ReasonSessionEnding == ReasonSessionEnding.Logoff ? "windows-logoff" : "windows-shutdown");
            base.OnSessionEnding(e);
            if (e.Cancel) AppLog.CancelExit();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                AppLog.Info("WPF exit entered; exitCode=" + e.ApplicationExitCode);
                _controller?.Dispose();
                _singleInstance?.Dispose();
                base.OnExit(e);
                AppLog.Finish(e.ApplicationExitCode);
            }
            catch (Exception error) { AppLog.Fatal("ExitCleanup", error); throw; }
            finally
            {
                try { Microsoft.Win32.SystemEvents.PowerModeChanged -= PowerModeChanged; }
                catch (Exception error) { AppLog.Error("PowerEventsUnsubscribe", error); }
            }
        }
    }
}
