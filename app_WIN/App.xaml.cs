using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using MeetingBar.Windows.Services;

namespace MeetingBar.Windows
{
    public partial class App : Application
    {
        private Mutex _singleInstance;
        private AppController _controller;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool created;
            _singleInstance = new Mutex(true, "Local\\MeetingBar.Windows", out created);
            if (!created) { SendCommand(e.Args.Length == 0 ? "popup" : e.Args[0].TrimStart('-')); Shutdown(); return; }
            base.OnStartup(e);
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(args.Exception.Message, "MeetingBar", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            _controller = new AppController();
            _controller.Start();
            if (e.Args.Length > 0) _controller.ExecuteCommand(e.Args[0].TrimStart('-'));
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

        protected override void OnExit(ExitEventArgs e)
        {
            _controller?.Dispose();
            _singleInstance?.Dispose();
            base.OnExit(e);
        }
    }
}
