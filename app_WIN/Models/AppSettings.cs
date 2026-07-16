using System.Collections.Generic;

namespace MeetingBar.Windows.Models
{
    public enum WidgetMode { TrayOnly, NearTray, TaskbarLeft }
    public enum CalendarProviderKind { Outlook, Ics, Google }
    public enum TitleMode { Full, Generic, Dot, Hidden }
    public enum TimeMode { Countdown, StartTime, Hidden }

    public sealed class AppSettings
    {
        public WidgetMode WidgetMode { get; set; } = WidgetMode.NearTray;
        public double WidgetWidth { get; set; } = 300;
        public double WidgetOffsetX { get; set; }
        public double WidgetOffsetY { get; set; }
        public TitleMode TitleMode { get; set; } = TitleMode.Full;
        public TimeMode TimeMode { get; set; } = TimeMode.Countdown;
        public int TitleMaxLength { get; set; } = 36;
        public bool ShowServiceIcon { get; set; } = true;
        public bool ShowAllDayEvents { get; set; } = true;
        public bool ShowDeclinedEvents { get; set; }
        public bool ShowTentativeEvents { get; set; } = true;
        public bool ShowFreeEvents { get; set; } = true;
        public bool RequireMeetingLink { get; set; }
        public bool IncludeTomorrow { get; set; } = true;
        public bool StartWithWindows { get; set; }
        public bool NotifyBeforeMeeting { get; set; } = true;
        public int NotifyMinutesBefore { get; set; } = 5;
        public bool NotifyAtEnd { get; set; }
        public bool FullscreenReminder { get; set; }
        public bool AutoJoin { get; set; }
        public int AutoJoinMinutesBefore { get; set; }
        public bool GlobalJoinHotkey { get; set; } = true;
        public int RefreshMinutes { get; set; } = 3;
        public CalendarProviderKind Provider { get; set; } = CalendarProviderKind.Outlook;
        public string IcsSources { get; set; } = "";
        public string GoogleClientId { get; set; } = "";
        public string GoogleClientSecret { get; set; } = "";
        public string GoogleRefreshTokenProtected { get; set; } = "";
        public string GoogleAccountEmail { get; set; } = "";
        public List<string> SelectedCalendarIds { get; set; } = new List<string>();
        public List<string> HiddenTitlePatterns { get; set; } = new List<string>();
        public List<string> CustomMeetingRegexes { get; set; } = new List<string>();
        public List<string> DismissedEventIds { get; set; } = new List<string>();
        public List<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public string CreateMeetingUrl { get; set; } = "https://meet.google.com/new";
        public string PreferredBrowserPath { get; set; } = "";
        public string PreferredBrowserArguments { get; set; } = "";
    }
}
