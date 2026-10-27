using System;
using System.Collections.Generic;
using System.Linq;
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

        public static Tuple<DateTime, DateTime> FullDayRange(bool includePastDay, bool includeNextDay, DateTime now)
            => FullDayRange(includePastDay ? 1 : 0, includeNextDay ? 1 : 0, now);

        public static Tuple<DateTime, DateTime> FullDayRange(int pastDays, int futureDays, DateTime now)
        {
            var start = now.Date.AddDays(-Math.Max(0, Math.Min(30, pastDays)));
            var end = now.Date.AddDays(Math.Max(0, Math.Min(30, futureDays)) + 1);
            return Tuple.Create(start, end);
        }

        public static IReadOnlyList<CalendarEvent> Page(IEnumerable<CalendarEvent> events, AppSettings settings, int dayOffset, DateTime now)
        {
            var dayStart = now.Date.AddDays(dayOffset);
            var dayEnd = dayStart.AddDays(1);
            var window = Range(settings, now);
            return (events ?? Enumerable.Empty<CalendarEvent>())
                .Where(item => dayOffset == 0
                    ? (settings.ShowFullDayEventList && item.End > dayStart && item.Start < dayEnd)
                      || item.End > window.Item1 && item.Start < window.Item2
                    : item.End > dayStart && item.Start < dayEnd)
                .OrderBy(item => item.Start).ThenBy(item => item.End).ToList();
        }

        public static CalendarEvent NextTaskbarEvent(IEnumerable<CalendarEvent> events, bool ignoreFreeEvents, DateTime now) =>
            (events ?? Enumerable.Empty<CalendarEvent>()).FirstOrDefault(item => item.End > now && !item.IsDismissed && (!ignoreFreeEvents || item.IsBusy));

        public static CalendarEvent NextWidgetEvent(IEnumerable<CalendarEvent> events, AppSettings settings, DateTime now)
        {
            var candidates = (events ?? Enumerable.Empty<CalendarEvent>())
                .Where(item => !item.IsAllDay || settings?.IncludeAllDayEventsInWidget == true && settings.ShowAllDayEvents)
                .OrderBy(item => item.IsAllDay).ThenBy(item => item.Start).ThenBy(item => item.End);
            var next = NextTaskbarEvent(candidates, settings?.IgnoreFreeEventsInTaskbar == true, now);
            if (next == null || settings == null || settings.EndOfDayWidgetBehavior == EndOfDayWidgetBehavior.NextEvent) return next;
            return next.Start.Date == now.Date || next.Start <= now ? next : null;
        }

        public static bool CanDismiss(CalendarEvent item, DateTime now) => item != null && !item.IsDismissed && item.End > now;
        public static string WidgetWhen(CalendarEvent item, DateTime now, bool use24HourClock)
        {
            if (item == null) return "";
            var day = item.Start.Date <= now.Date && item.End > now ? "Hoy" : item.Start.Date == now.Date.AddDays(1) ? "Mañana" : item.Start.ToString(item.Start.Year == now.Year ? "d MMM" : "d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-CR"));
            if (item.IsAllDay) return day + " · todo el día";
            if (item.Start <= now && item.End > now) return "Ahora · hasta " + CalendarEvent.FormatTime(item.End, use24HourClock);
            return day + ", " + CalendarEvent.FormatTime(item.Start, use24HourClock);
        }

        public static IReadOnlyList<CalendarEvent> GroupAgenda(IEnumerable<CalendarEvent> events) =>
            (events ?? Enumerable.Empty<CalendarEvent>()).OrderBy(item => item.Start.Date).ThenByDescending(item => item.IsAllDay).ThenBy(item => item.Start).ThenBy(item => item.End).ToList();
        public static bool ShouldRestoreDismissal(CalendarEvent item, DateTime now) => item != null && item.IsDismissed && item.End <= now;
    }
}
