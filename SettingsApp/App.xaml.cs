using Microsoft.UI.Xaml;

namespace MeetingBar.SettingsApp;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        MeetingBar.Localization.SpanishText.Initialize();
        InitializeComponent();
        UnhandledException += (_, args) =>
        {
            SettingsLog.Write("Unhandled: " + args.Exception.Message);
            args.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
