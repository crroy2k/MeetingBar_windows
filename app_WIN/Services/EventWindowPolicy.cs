using System;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public static class EventWindowPolicy
    {
        public static Tuple<DateTime, DateTime> Range(AppSettings settings, DateTime now) => Range(
            settings?.PastMeetingHours ?? 3,
            settings?.FutureMeetingHours ?? 24,
            settings?.IncludePastDay == true,
            settings?.IncludeNextDay != false,
            now);

        public static Tuple<DateTime, DateTime> Range(int pastHours, int futureHours, bool includePastDay, bool includeNextDay, DateTime now)
        {
            var start = now.AddHours(-Math.Max(0, Math.Min(168, pastHours)));
            var end = now.AddHours(Math.Max(1, Math.Min(168, futureHours)));
            if (!includePastDay && start.Date < now.Date) start = now.Date;
            if (!includeNextDay && end.Date > now.Date) end = now.Date.AddDays(1);
            if (end <= start) end = start.AddHours(1);
            return Tuple.Create(start, end);
        }
    }
}
