using System.Collections.Generic;

namespace MeetingBar.Windows.Models
{
    public enum WidgetMode { TrayOnly, NearTray, TaskbarLeft }
    public enum CalendarProviderKind { Outlook, Ics, Google }
    public enum TitleMode { Full, Generic, Dot, Hidden }
    public enum TimeMode { Countdown, StartTime, Hidden }
    public enum WidgetHighlightStyle { Hover, Shine }

    public sealed class AppSettings
    {
        public WidgetMode WidgetMode { get; set; } = WidgetMode.NearTray;
        public double WidgetWidth { get; set; } = 500;
        public double WidgetOffsetX { get; set; }
        public double WidgetOffsetY { get; set; }
        public bool UseAutomaticWidgetColor { get; set; } = true;
        public string WidgetBackgroundColor { get; set; } = "#202020";
        public int WidgetSettingsVersion { get; set; }
        public TitleMode TitleMode { get; set; } = TitleMode.Full;
        public TimeMode TimeMode { get; set; } = TimeMode.Countdown;
        public bool ShowOngoingTimeRemaining { get; set; } = true;
        public int TitleMaxLength { get; set; } = 64;
        public bool ShowServiceIcon { get; set; } = true;
        public bool EnableWinWWidget { get; set; }
        public bool HighlightStartingMeetings { get; set; } = true;
        public int HighlightMinutesBefore { get; set; } = 5;
        public int HighlightMinutesAfterStart { get; set; } = 10;
        public bool HighlightWholeMeeting { get; set; }
        public WidgetHighlightStyle WidgetHighlightStyle { get; set; } = WidgetHighlightStyle.Shine;
        public bool ShowAllDayEvents { get; set; } = true;
        public bool ShowDeclinedEvents { get; set; }
        public bool ShowTentativeEvents { get; set; } = true;
        public bool ShowFreeEvents { get; set; } = true;
        public bool IgnoreFreeEventsInTaskbar { get; set; }
        public bool RequireMeetingLink { get; set; }
        public bool IncludeTomorrow { get; set; } = true;
        public int PastMeetingHours { get; set; } = 3;
        public int FutureMeetingHours { get; set; } = 24;
        public bool IncludePastDay { get; set; }
        public bool IncludeNextDay { get; set; } = true;
        public int TimelinePastDays { get; set; } = 1;
        public int TimelineFutureDays { get; set; } = 1;
        public bool ShowFullDayEventList { get; set; } = true;
        public bool Use24HourClock { get; set; } = true;
        public bool ShowTimelineDate { get; set; } = true;
        public string TimelineDateFormat { get; set; } = "dd.MM.yyyy";
        public int EventWindowSettingsVersion { get; set; }
        public bool StartWithWindows { get; set; }
        public bool NotifyBeforeMeeting { get; set; } = true;
        public int NotifyMinutesBefore { get; set; } = 5;
        public List<int> NotificationMinutesBefore { get; set; } = new List<int> { 5 };
        public int NotificationScheduleVersion { get; set; }
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
        public string MapProvider { get; set; } = "Google Maps";
    }
}
