using System;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public static class WidgetHighlightPolicy
    {
        public static bool ShouldHighlight(CalendarEvent item, AppSettings settings, DateTime now)
        {
            if (item == null || !settings.HighlightStartingMeetings) return false;
            if (now < item.Start.AddMinutes(-Math.Max(0, settings.HighlightMinutesBefore)) || now >= item.End) return false;
            if (now < item.Start || settings.HighlightWholeMeeting) return true;
            return now < item.Start.AddMinutes(Math.Max(1, settings.HighlightMinutesAfterStart));
        }
    }
}
