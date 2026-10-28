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
