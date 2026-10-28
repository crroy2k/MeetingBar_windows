using System;
using System.Globalization;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace MeetingBar.Windows.Models
{
    public sealed class CalendarEvent : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
        public string Location { get; set; }
        public string EventUrl { get; set; }
        public string CalendarOpenUrl { get; set; }
        public string ConferenceUrl { get; set; }
        public string SourceId { get; set; }
        public string CalendarSourceName { get; set; }
        public string CalendarId { get; set; }
        public string CalendarName { get; set; }
        public string CalendarColor { get; set; } = "#6D7CFF";
        public string Organizer { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsAllDay { get; set; }
        public bool IsDeclined { get; set; }
        public bool IsTentative { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsBusy { get; set; } = true;
        public string MeetingUrl { get; set; }
        public string MeetingService { get; set; }
        public bool IsDismissed { get; set; }
        public bool Use24HourClock { get; set; } = true;
        public bool ShowDayDivider { get; set; }
        public string AgendaSectionHeader { get; set; }
        public bool ShowAgendaSectionHeader => !string.IsNullOrEmpty(AgendaSectionHeader);

        public bool IsCurrent => Start <= DateTime.Now && End > DateTime.Now;
        public bool IsPast => End <= DateTime.Now;
        public bool CanJoin => !string.IsNullOrWhiteSpace(MeetingUrl);
        public bool CanRestoreNotifications => IsDismissed && !IsPast;
        public bool CanOpenDetails => !string.IsNullOrWhiteSpace(CalendarOpenUrl);
        public string StartTimeText => IsAllDay ? "Todo el día" : FormatTime(Start, Use24HourClock);
        public string TimeRange => IsAllDay ? "Todo el día" : FormatTime(Start, Use24HourClock) + " – " + FormatTime(End, Use24HourClock);
        public string DayLabel => Start.Date == DateTime.Today ? "HOY" : Start.Date == DateTime.Today.AddDays(1) ? "MAÑANA" : Start.ToString("ddd, d MMM", MeetingBar.Localization.SpanishText.Culture).ToUpperInvariant();
        public static string FormatTime(DateTime value, bool use24HourClock, bool hourOnly = false) => value.ToString(
            use24HourClock ? (hourOnly ? "HH" : "HH:mm") : (hourOnly ? "h tt" : "h:mm tt"),
            MeetingBar.Localization.SpanishText.Culture);
        public static string FormatDate(DateTime value, string format)
        {
            switch (format)
            {
                case "MM/dd/yyyy": case "dd/MM/yyyy": case "yyyy-MM-dd": case "dd MMM yyyy": break;
                default: format = "dd.MM.yyyy"; break;
            }
            return value.ToString(format, MeetingBar.Localization.SpanishText.Culture);
        }
        public string CalendarDisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CalendarName)) return MeetingBar.Localization.SpanishText.SourceName(CalendarSourceName);
                if (string.IsNullOrWhiteSpace(CalendarSourceName) || string.Equals(CalendarName, CalendarSourceName, StringComparison.OrdinalIgnoreCase)) return MeetingBar.Localization.SpanishText.SourceName(CalendarName);
                return CalendarName + " · " + MeetingBar.Localization.SpanishText.SourceName(CalendarSourceName);
            }
        }
        public string CalendarDetails => string.IsNullOrWhiteSpace(CalendarDisplayName) ? null : "Calendario  ·  " + CalendarDisplayName;
        public string OrganizerDetails => string.IsNullOrWhiteSpace(Organizer) ? null : "Organizador  ·  " + Organizer;
        public string LocationDetails => string.IsNullOrWhiteSpace(Location) ? null : "Ubicación  ·  " + Location;
        public string MeetingDetails => string.IsNullOrWhiteSpace(MeetingService) ? null : "Enlace de reunión  ·  " + MeetingService;
        public string DetailsNotes => string.IsNullOrWhiteSpace(Notes) ? null : WebUtility.HtmlDecode(Regex.Replace(Notes, "<[^>]+>", " ")).Trim();
        public string Countdown
        {
            get
            {
                if (IsCurrent) return "Ahora";
                if (IsPast)
                {
                    return "Finalizada";
                }
                var span = Start - DateTime.Now;
                if (span.TotalMinutes < 1) return "En breve";
                if (span.TotalHours < 1) return "En " + Math.Ceiling(span.TotalMinutes) + " min";
                if (span.TotalDays < 1) return "En " + Math.Floor(span.TotalHours) + " h " + span.Minutes + " min";
                return "En " + Math.Floor(span.TotalDays) + " d";
            }
        }
        public string DetailedCountdown
        {
            get
            {
                if (!IsPast) return Countdown;
                var elapsed = DateTime.Now - End;
                if (elapsed.TotalMinutes < 1) return "Acaba de finalizar";
                if (elapsed.TotalHours < 1) return "Finalizó hace " + Math.Floor(elapsed.TotalMinutes) + " min";
                if (elapsed.TotalDays < 1) return "Finalizó hace " + Math.Floor(elapsed.TotalHours) + " h " + elapsed.Minutes + " min";
                return "Finalizó hace " + Math.Floor(elapsed.TotalDays) + " d";
            }
        }
        public string Remaining
        {
            get
            {
                if (!IsCurrent) return Countdown;
                var remaining = End - DateTime.Now;
                if (remaining.TotalMinutes < 1) return "Menos de 1 min restante";
                if (remaining.TotalHours < 1) return Math.Ceiling(remaining.TotalMinutes) + " min restantes";
                return Math.Floor(remaining.TotalHours) + " h " + remaining.Minutes + " min restantes";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RefreshComputed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
