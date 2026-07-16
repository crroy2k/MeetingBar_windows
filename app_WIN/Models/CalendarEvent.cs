using System;
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

        public bool IsCurrent => Start <= DateTime.Now && End > DateTime.Now;
        public bool IsPast => End <= DateTime.Now;
        public bool CanJoin => !string.IsNullOrWhiteSpace(MeetingUrl);
        public bool CanOpenDetails => !string.IsNullOrWhiteSpace(CalendarOpenUrl);
        public string TimeRange => IsAllDay ? "All day" : Start.ToString("t") + " – " + End.ToString("t");
        public string DayLabel => Start.Date == DateTime.Today ? "TODAY" : Start.Date == DateTime.Today.AddDays(1) ? "TOMORROW" : Start.ToString("ddd, d MMM").ToUpperInvariant();
        public string CalendarDetails => string.IsNullOrWhiteSpace(CalendarName) ? null : "Calendar  ·  " + CalendarName;
        public string OrganizerDetails => string.IsNullOrWhiteSpace(Organizer) ? null : "Organizer  ·  " + Organizer;
        public string LocationDetails => string.IsNullOrWhiteSpace(Location) ? null : "Location  ·  " + Location;
        public string MeetingDetails => string.IsNullOrWhiteSpace(MeetingService) ? null : "Meeting link  ·  " + MeetingService;
        public string DetailsNotes => string.IsNullOrWhiteSpace(Notes) ? null : WebUtility.HtmlDecode(Regex.Replace(Notes, "<[^>]+>", " ")).Trim();
        public string Countdown
        {
            get
            {
                if (IsCurrent) return "Now";
                if (IsPast)
                {
                    var elapsed = DateTime.Now - End;
                    if (elapsed.TotalMinutes < 1) return "Just ended";
                    if (elapsed.TotalHours < 1) return "Ended " + Math.Floor(elapsed.TotalMinutes) + "m ago";
                    if (elapsed.TotalDays < 1) return "Ended " + Math.Floor(elapsed.TotalHours) + "h " + elapsed.Minutes + "m ago";
                    return "Ended " + Math.Floor(elapsed.TotalDays) + "d ago";
                }
                var span = Start - DateTime.Now;
                if (span.TotalMinutes < 1) return "Soon";
                if (span.TotalHours < 1) return "in " + Math.Ceiling(span.TotalMinutes) + "m";
                if (span.TotalDays < 1) return "in " + Math.Floor(span.TotalHours) + "h " + span.Minutes + "m";
                return "in " + Math.Floor(span.TotalDays) + "d";
            }
        }
        public string Remaining
        {
            get
            {
                if (!IsCurrent) return Countdown;
                var remaining = End - DateTime.Now;
                if (remaining.TotalMinutes < 1) return "Less than 1m left";
                if (remaining.TotalHours < 1) return Math.Ceiling(remaining.TotalMinutes) + "m left";
                return Math.Floor(remaining.TotalHours) + "h " + remaining.Minutes + "m left";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RefreshComputed() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}
