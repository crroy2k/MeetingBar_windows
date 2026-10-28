using System;
using System.Diagnostics;
using System.Linq;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public sealed class MeetingOpener
    {
        private readonly SettingsService _settings;
        public MeetingOpener(SettingsService settings) { _settings = settings; }

        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!IsAllowedUrl(url)) throw new InvalidOperationException("El tipo de enlace no es compatible o no es seguro.");
            var s = _settings.Current;
            if (!string.IsNullOrWhiteSpace(s.PreferredBrowserPath))
            {
                var arguments = string.IsNullOrWhiteSpace(s.PreferredBrowserArguments) ? Quote(url) : s.PreferredBrowserArguments.Replace("{url}", Quote(url));
                Process.Start(new ProcessStartInfo(s.PreferredBrowserPath, arguments) { UseShellExecute = true });
            }
            else Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public bool IsCreationMenuOpen { get; private set; }
        public void CreateMeeting(System.Windows.FrameworkElement anchor = null)
        {
            var menu = new System.Windows.Controls.ContextMenu();
            foreach (var service in _settings.Current.MeetingServices ?? new System.Collections.Generic.List<Bookmark>())
            {
                if (service == null || string.IsNullOrWhiteSpace(service.Name)) continue;
                var entry = new System.Windows.Controls.MenuItem { Header = service.Name, ToolTip = service.Url };
                entry.Click += (s, e) => {
                    try {
                        if (!IsCreationUrl(service.Url)) throw new InvalidOperationException("Configura una URL HTTPS válida para este servicio en Ajustes → Reuniones.");
                        Open(service.Url);
                    }
                    catch (Exception error) { System.Windows.MessageBox.Show(error.Message, "Crear reunión"); }
                };
                menu.Items.Add(entry);
            }
            menu.Items.Add(new System.Windows.Controls.Separator());
            var configure = new System.Windows.Controls.MenuItem { Header = "Configurar servicios…" };
            configure.Click += (s, e) => ConfigureMeetingServices?.Invoke();
            menu.Items.Add(configure);
            menu.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40));
            menu.Foreground = System.Windows.Media.Brushes.White;
            menu.PlacementTarget = anchor;
            menu.Placement = anchor == null ? System.Windows.Controls.Primitives.PlacementMode.MousePoint : System.Windows.Controls.Primitives.PlacementMode.Top;
            menu.Closed += (s, e) => IsCreationMenuOpen = false;
            IsCreationMenuOpen = true;
            menu.IsOpen = true;
        }
        public event Action ConfigureMeetingServices;
        internal static bool IsCreationUrl(string url) => Uri.IsWellFormedUriString(url, UriKind.Absolute) && Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && !string.IsNullOrWhiteSpace(uri.Host);

        public void Open(CalendarEvent item) => Open(item?.MeetingUrl);

        internal static CalendarProviderKind CalendarProviderFor(AppSettings settings, CalendarEvent item)
        {
            var source = settings.CalendarSources?.FirstOrDefault(x => x != null && x.Enabled && x.Id == item?.SourceId)
                ?? settings.CalendarSources?.FirstOrDefault(x => x != null && x.Enabled);
            return source?.Provider ?? settings.Provider;
        }

        public bool CanOpenCalendar(CalendarEvent item = null) =>
            CalendarProviderFor(_settings.Current, item) != CalendarProviderKind.Ics
            || !string.IsNullOrWhiteSpace(item?.CalendarOpenUrl) || !string.IsNullOrWhiteSpace(item?.EventUrl);

        public void OpenCalendar(CalendarEvent item = null)
        {
            switch (CalendarProviderFor(_settings.Current, item))
            {
                case CalendarProviderKind.Outlook:
                    var belongsToOutlook = item != null && _settings.Current.CalendarSources.Any(x => x != null
                        && x.Enabled && x.Provider == CalendarProviderKind.Outlook && x.Id == item.SourceId);
                    OutlookCalendarNavigator.Open(belongsToOutlook ? item.CalendarId : null);
                    break;
                case CalendarProviderKind.Google:
                    Open(item?.CalendarOpenUrl ?? "https://calendar.google.com/calendar/u/0/r");
                    break;
                default:
                    Open(item?.CalendarOpenUrl ?? item?.EventUrl);
                    break;
            }
        }
        internal static bool IsAllowedUrl(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return false;
            return uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "zoommtg" || uri.Scheme == "discord" || uri.Scheme == "outlookcal";
        }
        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
