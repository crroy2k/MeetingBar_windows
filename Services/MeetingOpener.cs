using System;
using System.Diagnostics;
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
            if (!IsAllowedUrl(url)) throw new InvalidOperationException("Unsupported or unsafe link type.");
            var s = _settings.Current;
            if (!string.IsNullOrWhiteSpace(s.PreferredBrowserPath))
            {
                var arguments = string.IsNullOrWhiteSpace(s.PreferredBrowserArguments) ? Quote(url) : s.PreferredBrowserArguments.Replace("{url}", Quote(url));
                Process.Start(new ProcessStartInfo(s.PreferredBrowserPath, arguments) { UseShellExecute = true });
            }
            else Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public void Open(CalendarEvent item) => Open(item?.MeetingUrl);
        internal static bool IsAllowedUrl(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return false;
            return uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "zoommtg" || uri.Scheme == "discord" || uri.Scheme == "outlookcal";
        }
        private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
