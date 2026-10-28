using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public static class DebugEventFactory
    {
        private static readonly Dictionary<string, string> Links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Google Meet"] = "https://meet.google.com/debug-test",
            ["Microsoft Teams"] = "https://teams.microsoft.com/l/meetup-join/debug-test",
            ["Zoom"] = "https://zoom.us/j/123456789",
            ["Sin enlace de reunión"] = null
        };

        public static CalendarEvent Create(string title, int startMinutes, int durationMinutes, string service, string color, DateTime now)
        {
            string link;
            Links.TryGetValue(service ?? "", out link);
            var start = now.AddMinutes(Math.Max(-1440, Math.Min(1440, startMinutes)));
            return new CalendarEvent
            {
                Id = "debug-" + Guid.NewGuid().ToString("N"),
                Title = string.IsNullOrWhiteSpace(title) ? "Reunión de prueba" : title.Trim(),
                Start = start,
                End = start.AddMinutes(Math.Max(1, Math.Min(1440, durationMinutes))),
                CalendarId = "meetingbar-debug",
                CalendarName = "Pruebas de MeetingBar",
                CalendarColor = Regex.IsMatch(color ?? "", "^#[0-9A-Fa-f]{6}$") ? color : "#0078D4",
                Organizer = "debug@meetingbar.local",
                MeetingService = string.Equals(service, "Sin enlace de reunión", StringComparison.OrdinalIgnoreCase) ? null : service,
                MeetingUrl = link,
                ConferenceUrl = link,
                Notes = "Evento local creado desde Configuración. No se guarda en tu calendario."
            };
        }
    }
}
