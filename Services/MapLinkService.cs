using System;

namespace MeetingBar.Windows.Services
{
    public static class MapLinkService
    {
        public static bool HasPhysicalLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location)) return false;
            Uri uri;
            if (Uri.TryCreate(location.Trim(), UriKind.Absolute, out uri)) return false;
            return MeetingLinkDetector.DetectText(location) == null;
        }

        public static string BuildUrl(string provider, string location)
        {
            if (!HasPhysicalLocation(location)) return null;
            var query = Uri.EscapeDataString(location.Trim());
            switch (provider)
            {
                case "Bing Maps": return "https://www.bing.com/maps?q=" + query;
                case "OpenStreetMap": return "https://www.openstreetmap.org/search?query=" + query;
                default: return "https://www.google.com/maps/search/?api=1&query=" + query;
            }
        }
    }
}
