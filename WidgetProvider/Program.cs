using Microsoft.Windows.Widgets.Providers;
namespace MeetingBar.WidgetProvider;
internal static class Program
{
    [MTAThread] private static void Main(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "-RegisterProcessAsComServer", StringComparison.OrdinalIgnoreCase)) return;
        WinRT.ComWrappersSupport.InitializeComWrappers();
        using var registration = RegistrationManager<MeetingBarWidgetProvider>.RegisterProvider();
        registration.GetDisposedEvent().WaitOne();
    }
}
