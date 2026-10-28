using System;
using System.Collections.Generic;
using System.Linq;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.Services;
using MeetingBar.Windows.Views;

namespace MeetingBar.Tests
{
    internal static class Program
    {
        private static int _failed;
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--render-widget") { RenderWidget(args[1]); return; }
            if (args.Length > 0 && args[0] == "--render-fallback") { RenderWidget(args[1], true); return; }
            if (args.Length > 0 && args[0] == "--render-agenda") { RenderAgenda(args[1]); return; }
            Test("meeting services migrate a legacy URL once and allow removing all services", () =>
            {
                var settings = new AppSettings { CreateMeetingUrl = "https://calendar.example.org/schedule#new" };
                settings.EnsureMeetingServices();
                Equal(1, settings.MeetingServices.Count);
                Equal(settings.CreateMeetingUrl, settings.MeetingServices[0].Url);
                settings.MeetingServices.Clear(); settings.EnsureMeetingServices();
                Equal(0, settings.MeetingServices.Count);
                var fresh = new AppSettings(); fresh.EnsureMeetingServices(); Equal(0, fresh.MeetingServices.Count);
                var json = new System.Web.Script.Serialization.JavaScriptSerializer();
                var restored = json.Deserialize<AppSettings>(json.Serialize(settings)); restored.EnsureMeetingServices();
                Equal(0, restored.MeetingServices.Count);
            });
            Test("creation URLs accept corporate HTTPS fragments and reject unsafe schemes and credentials", () =>
            {
                Equal(true, MeetingOpener.IsCreationUrl("https://tenant.example.org/#/conferences/schedule/new"));
                Equal(false, MeetingOpener.IsCreationUrl("http://tenant.example.org/new"));
                Equal(false, MeetingOpener.IsCreationUrl("https://user:password@example.org/new"));
                Equal(false, MeetingOpener.IsCreationUrl("file:///C:/Windows/System32/cmd.exe"));
                Equal(false, MeetingOpener.IsCreationUrl("javascript:alert(1)"));
                Equal(false, MeetingOpener.IsCreationUrl(null));
            });
            Test("update versions compare numerically and reject prerelease tags", UpdateTests.VersionPolicy);
            Test("update metadata requires this repo installer and SHA-256", UpdateTests.MetadataPolicy);
            Test("update redirects reject foreign hosts credentials and HTTP", UpdateTests.RedirectPolicy);
            Test("updates check only on demand and bootstrap old releases", () => UpdateTests.OnDemandAndBootstrap().GetAwaiter().GetResult());
            Test("update download verifies bytes before executable validation and rejects downgrades", () => UpdateTests.DownloadIntegrity().GetAwaiter().GetResult());
            Test("updates reject redirected and oversized metadata", () => UpdateTests.RejectRedirectAndOversize().GetAwaiter().GetResult());
            Test("calendar navigation follows the event source rather than legacy provider settings", () =>
            {
                var outlook = CalendarSourceSettings.Create(CalendarProviderKind.Outlook);
                var google = CalendarSourceSettings.Create(CalendarProviderKind.Google);
                var ics = CalendarSourceSettings.Create(CalendarProviderKind.Ics);
                var settings = new AppSettings { Provider = CalendarProviderKind.Google, CalendarSources = new List<CalendarSourceSettings> { outlook, google, ics } };
                Equal(CalendarProviderKind.Outlook, MeetingOpener.CalendarProviderFor(settings, null));
                Equal(CalendarProviderKind.Outlook, MeetingOpener.CalendarProviderFor(settings, new CalendarEvent { SourceId = outlook.Id }));
                Equal(CalendarProviderKind.Google, MeetingOpener.CalendarProviderFor(settings, new CalendarEvent { SourceId = google.Id }));
                Equal(CalendarProviderKind.Ics, MeetingOpener.CalendarProviderFor(settings, new CalendarEvent { SourceId = ics.Id }));
                outlook.Enabled = false;
                Equal(CalendarProviderKind.Google, MeetingOpener.CalendarProviderFor(settings, null));
            });
            Test("Spanish labels do not replace user text or calendar identifiers", () =>
            {
                Equal("Outlook clásico", MeetingBar.Localization.SpanishText.SourceName("Classic Outlook"));
                Equal("Mi calendario de trabajo", MeetingBar.Localization.SpanishText.SourceName("Mi calendario de trabajo"));
                Equal("Terminé por hoy", MeetingBar.Localization.SpanishText.EndOfDayMessage("Terminé por hoy"));
                Equal("Jornada finalizada", MeetingBar.Localization.SpanishText.EndOfDayMessage("Done for today"));
                var settings = new AppSettings { CalendarSourcesVersion = 1, CalendarSources = new List<CalendarSourceSettings> {
                    new CalendarSourceSettings { Id = "stable-id", Name = "Classic Outlook", Provider = CalendarProviderKind.Outlook, SelectedCalendarIds = new List<string> { "calendar-id" }, CalendarSelectionConfigured = true },
                    new CalendarSourceSettings { Id = "custom-id", Name = "Mi trabajo", Provider = CalendarProviderKind.Outlook } } };
                settings.EnsureCalendarSources();
                Equal("Outlook clásico", settings.CalendarSources[0].Name);
                Equal("stable-id", settings.CalendarSources[0].Id);
                Equal("calendar-id", settings.CalendarSources[0].SelectedCalendarIds[0]);
                Equal("Mi trabajo", settings.CalendarSources[1].Name);
                Equal("HOY", new CalendarEvent { Start = DateTime.Today }.DayLabel);
                Equal("Todo el día", new CalendarEvent { IsAllDay = true }.TimeRange);
            });
            Test("monitor selection survives disconnect and reconnect", () =>
            {
                var primary = new MeetingBar.Display.MonitorChoice { DeviceName = "primary", Primary = true };
                var secondary = new MeetingBar.Display.MonitorChoice { DeviceName = "secondary", Left = -1920, Right = 0 };
                Equal(secondary, MeetingBar.Display.MonitorCatalog.Resolve(new[] { primary, secondary }, "SECONDARY"));
                Equal(primary, MeetingBar.Display.MonitorCatalog.Resolve(new[] { primary }, "secondary"));
                Equal(primary, MeetingBar.Display.MonitorCatalog.Resolve(new[] { secondary, primary }, ""));
                Equal<MeetingBar.Display.MonitorChoice>(null, MeetingBar.Display.MonitorCatalog.Resolve(new MeetingBar.Display.MonitorChoice[0], "secondary"));
                Equal(secondary, MeetingBar.Display.MonitorCatalog.Resolve(new[] { primary, secondary }, "secondary"));
            });
            Test("timed appointments win over earlier all-day events", () =>
            {
                var now = new DateTime(2026, 10, 27, 8, 0, 0);
                var holiday = new CalendarEvent { Id = "holiday", Start = now.Date, End = now.Date.AddDays(1), IsAllDay = true };
                var appointment = new CalendarEvent { Id = "appointment", Start = now.AddDays(2), End = now.AddDays(2).AddHours(1) };
                var settings = new AppSettings();
                Equal(appointment, EventWindowPolicy.NextWidgetEvent(new[] { holiday, appointment }, settings, now));
                Equal<CalendarEvent>(null, EventWindowPolicy.NextWidgetEvent(new[] { holiday }, settings, now));
                settings.IncludeAllDayEventsInWidget = true;
                Equal(appointment, EventWindowPolicy.NextWidgetEvent(new[] { holiday, appointment }, settings, now));
                Equal(holiday, EventWindowPolicy.NextWidgetEvent(new[] { holiday }, settings, now));
                Equal(2, EventWindowPolicy.GroupAgenda(new[] { holiday, appointment }).Count);
            });
            Test("compact dates distinguish today tomorrow future and ongoing appointments", () =>
            {
                var now = new DateTime(2026, 10, 27, 8, 0, 0);
                var item = new CalendarEvent { Start = now.AddHours(1), End = now.AddHours(2) };
                Equal("Hoy, 09:00", EventWindowPolicy.WidgetWhen(item, now, true));
                item.Start = now.AddDays(1).AddHours(1); item.End = item.Start.AddHours(1);
                Equal("Mañana, 09:00", EventWindowPolicy.WidgetWhen(item, now, true));
                item.Start = new DateTime(2026, 11, 12, 9, 0, 0); item.End = item.Start.AddHours(1);
                Contains("12 nov", EventWindowPolicy.WidgetWhen(item, now, true));
                item.Start = now.AddHours(-1); item.End = now.AddHours(1);
                Equal("Ahora · hasta 09:00", EventWindowPolicy.WidgetWhen(item, now, true));
                item.IsAllDay = true;
                Equal("Hoy · todo el día", EventWindowPolicy.WidgetWhen(item, now, true));
                Equal("Todo el día", item.StartTimeText);
            });
            Test("agenda groups all-day events separately within each day", () =>
            {
                var today = new DateTime(2026, 10, 27);
                var holiday = new CalendarEvent { Id = "holiday", Start = today, End = today.AddDays(1), IsAllDay = true };
                var early = new CalendarEvent { Id = "early", Start = today.AddHours(8), End = today.AddHours(9) };
                var later = new CalendarEvent { Id = "later", Start = today.AddHours(10), End = today.AddHours(11) };
                var grouped = EventWindowPolicy.GroupAgenda(new[] { later, early, holiday });
                Equal("holiday", grouped[0].Id); Equal("early", grouped[1].Id); Equal("later", grouped[2].Id);
            });
            Test("hybrid timing switches below 24 hours and defaults to 12-hour time", () =>
            {
                var now = new DateTime(2026, 10, 27, 14, 0, 0);
                var item = new CalendarEvent { Start = now.AddHours(24), End = now.AddHours(25) };
                Equal(false, new AppSettings().Use24HourClock);
                Equal("En 1 d", EventWindowPolicy.WidgetTiming(item, now, false));
                item.Start = now.AddHours(23); item.End = item.Start.AddHours(1);
                Equal("Mañana, 1:00 p. m.", EventWindowPolicy.WidgetTiming(item, now, false));
                item.Start = now.AddMinutes(30); item.End = item.Start.AddHours(1);
                Equal("Hoy, 2:30 p. m.", EventWindowPolicy.WidgetTiming(item, now, false));
                item.Start = now.AddMinutes(-30); item.End = now.AddMinutes(45);
                Equal("Quedan 45 min", EventWindowPolicy.WidgetTiming(item, now, false));
            });
            Test("informational links are not join targets", () =>
            {
                Equal<MeetingLinkResult>(null, MeetingLinkDetector.DetectText("Privacy https://example.com/privacy"));
                Equal(false, MeetingOpener.IsAllowedUrl("file:///C:/Windows/System32/cmd.exe"));
                Equal(false, MeetingOpener.IsAllowedUrl("javascript:alert(1)"));
                Equal(true, MeetingOpener.IsAllowedUrl("https://teams.microsoft.com/l/meetup-join/example"));
            });
            Test("recurring Outlook occurrences have distinct identities", () =>
            {
                var first = OutlookCalendarProvider.OccurrenceId("series", new DateTime(2026, 10, 27, 9, 0, 0));
                var next = OutlookCalendarProvider.OccurrenceId("series", new DateTime(2026, 10, 28, 9, 0, 0));
                Equal(false, first == next);
                Equal(first, OutlookCalendarProvider.OccurrenceId("series", new DateTime(2026, 10, 27, 9, 0, 0)));
            });
            Test("detects Teams links", () =>
            {
                var found = MeetingLinkDetector.DetectText("Join https://teams.microsoft.com/l/meetup-join/19%3ameeting_abc%40thread.v2/0?context=x");
                Equal("Microsoft Teams", found.Service); Equal("https://teams.microsoft.com/l/meetup-join/19%3ameeting_abc%40thread.v2/0?context=x", found.Url);
            });
            Test("conference data wins over notes", () =>
            {
                var item = new CalendarEvent { ConferenceUrl = "https://meet.google.com/abc-defg-hij", Notes = "backup https://zoom.us/j/123456789" };
                var found = MeetingLinkDetector.Detect(item); Equal("Google Meet", found.Service);
            });
            Test("unwraps Outlook SafeLinks", () =>
            {
                var wrapped = "https://nam.safelinks.protection.outlook.com/?url=https%3A%2F%2Fmeet.google.com%2Fabc-defg-hij&data=foo";
                Contains("https://meet.google.com/abc-defg-hij", MeetingLinkDetector.UnwrapSafeLinks(wrapped));
            });
            Test("parses and expands daily ICS recurrence", () =>
            {
                var ics = "BEGIN:VCALENDAR\nX-WR-CALNAME:Work\nBEGIN:VEVENT\nUID:daily\nSUMMARY:Standup\nDTSTART:20260716T100000\nDTEND:20260716T103000\nRRULE:FREQ=DAILY;COUNT=3\nDESCRIPTION:Join https://meet.google.com/abc-defg-hij\nEND:VEVENT\nEND:VCALENDAR";
                var events = IcsCalendarProvider.Parse(ics, "work", "Work", "#fff", new DateTime(2026, 7, 16), new DateTime(2026, 7, 20));
                Equal(3, events.Count); Equal(new DateTime(2026, 7, 18, 10, 0, 0), events.Last().Start);
            });
            Test("normalizes webcal subscriptions to secure feed URLs", () =>
            {
                Equal("https://calendar.example.com/team.ics?token=secret", IcsCalendarProvider.NormalizeRemoteUri("webcal://calendar.example.com/team.ics?token=secret").AbsoluteUri);
                Equal("https://calendar.example.com/team.ics", IcsCalendarProvider.NormalizeRemoteUri("webcals://calendar.example.com/team.ics").AbsoluteUri);
                Equal("http://calendar.example.com/team.ics", IcsCalendarProvider.NormalizeRemoteUri("http://calendar.example.com/team.ics").AbsoluteUri);
                Equal(null, IcsCalendarProvider.NormalizeRemoteUri("C:\\Calendars\\team.ics"));
            });
            Test("defaults retain privacy-safe local provider", () =>
            {
                var settings = new AppSettings();
                Equal(true, settings.EnsureCalendarSources());
                Equal(1, settings.CalendarSources.Count);
                Equal(CalendarProviderKind.Outlook, settings.CalendarSources[0].Provider);
            });
            Test("legacy Google settings migrate into an independent source", () =>
            {
                var settings = new AppSettings
                {
                    Provider = CalendarProviderKind.Google,
                    GoogleRefreshTokenProtected = "protected-token",
                    GoogleAccountEmail = "one@example.com",
                    SelectedCalendarIds = new List<string> { "primary" }
                };
                settings.EnsureCalendarSources();
                var source = settings.CalendarSources.Single();
                Equal(CalendarProviderKind.Google, source.Provider);
                Equal("protected-token", source.GoogleRefreshTokenProtected);
                Equal("one@example.com", source.GoogleAccountEmail);
                Equal(true, source.CalendarSelectionConfigured);
                Equal("primary", source.SelectedCalendarIds.Single());
                Equal(false, settings.EnsureCalendarSources());
                Equal(1, settings.CalendarSources.Count);
            });
            Test("source namespaces prevent cross-account event collisions", () =>
            {
                Equal("account-one::primary::same-event", CalendarSyncService.SourceEventId("account-one", "primary", "same-event"));
                Equal(false, CalendarSyncService.SourceEventId("account-one", "primary", "same-event") == CalendarSyncService.SourceEventId("account-two", "primary", "same-event"));
                Equal(false, CalendarSyncService.SourceEventId("account-one", "primary", "same-event") == CalendarSyncService.SourceEventId("account-one", "shared", "same-event"));
            });
            Test("events identify their calendar account", () =>
            {
                var item = new CalendarEvent { CalendarName = "Team", CalendarSourceName = "one@example.com" };
                Equal("Team · one@example.com", item.CalendarDisplayName);
            });
            Test("Win+W widget stays opt-in", () => Equal(false, new AppSettings().EnableWinWWidget));
            Test("taskbar widget keeps next-event behavior by default", () => Equal(EndOfDayWidgetBehavior.NextEvent, new AppSettings().EndOfDayWidgetBehavior));
            Test("fullscreen widget hiding stays opt-in", () =>
            {
                var settings = new AppSettings();
                Equal(false, settings.HideWidgetInFullscreen);
                Equal(0, settings.FullscreenAppBlacklist.Count);
            });
            Test("fullscreen detection distinguishes monitor coverage from maximized windows", () =>
            {
                var monitor = new System.Drawing.Rectangle(0, 0, 1920, 1080);
                Equal(true, FullscreenWidgetVisibilityService.CoversMonitor(new System.Drawing.Rectangle(0, 0, 1920, 1080), monitor));
                Equal(true, FullscreenWidgetVisibilityService.CoversMonitor(new System.Drawing.Rectangle(-1, -1, 1922, 1082), monitor));
                Equal(false, FullscreenWidgetVisibilityService.CoversMonitor(new System.Drawing.Rectangle(0, 0, 1920, 1040), monitor));
            });
            Test("fullscreen app blacklist prefers exact paths with a process-name fallback", () =>
            {
                var blacklist = new[] { "C:\\Games\\Example\\game.exe" };
                Equal(true, FullscreenWidgetVisibilityService.MatchesBlacklist(blacklist, "c:\\games\\example\\GAME.exe", "game"));
                Equal(false, FullscreenWidgetVisibilityService.MatchesBlacklist(blacklist, "C:\\Other\\game.exe", "game"));
                Equal(true, FullscreenWidgetVisibilityService.MatchesBlacklist(blacklist, null, "GAME"));
            });
            Test("requires Google Calendar read-only scope", () => { Equal(true, GoogleCalendarProvider.HasCalendarScope("openid https://www.googleapis.com/auth/calendar.readonly")); Equal(false, GoogleCalendarProvider.HasCalendarScope("openid https://www.googleapis.com/auth/userinfo.email")); });
            Test("explains disabled Google Calendar API", () => Contains("no está habilitada", GoogleCalendarProvider.ApiErrorMessage(System.Net.HttpStatusCode.Forbidden, "{\"error\":{\"errors\":[{\"reason\":\"accessNotConfigured\"}]}}")));
            Test("maps Google event-specific colors", () =>
            {
                var colors = new Dictionary<string, object> { ["event"] = new Dictionary<string, object> { ["5"] = new Dictionary<string, object> { ["background"] = "#f6bf26" } } };
                Equal("#f6bf26", GoogleCalendarProvider.ResolveEventColor("5", GoogleCalendarProvider.ParseEventColors(colors), "#6D7CFF"));
                Equal("#6D7CFF", GoogleCalendarProvider.ResolveEventColor(null, GoogleCalendarProvider.ParseEventColors(colors), "#6D7CFF"));
            });
            Test("groups color emoji sequences", () =>
            {
                var keys = EmojiTokenizer.Tokenize("🔒 🤝 👩‍💻").Where(item => item.Key != null).Select(item => item.Key).ToArray();
                Equal("1f512", keys[0]); Equal("1f91d", keys[1]); Equal("1f469-200d-1f4bb", keys[2]);
            });
            Test("ongoing and past meeting countdowns stay accurate", () =>
            {
                var current = new CalendarEvent { Start = DateTime.Now.AddMinutes(-10), End = DateTime.Now.AddMinutes(25) };
                var past = new CalendarEvent { Start = DateTime.Now.AddHours(-2), End = DateTime.Now.AddMinutes(-20) };
                Contains("restantes", current.Remaining); Equal("Finalizada", past.Countdown); Contains("hace", past.DetailedCountdown);
            });
            Test("multiple notification times stay independent", () =>
            {
                var settings = new AppSettings { NotificationScheduleVersion = 1, NotificationMinutesBefore = new List<int> { 0, 5, 5, 15, -1, 121 } };
                Equal("15,5,0", string.Join(",", NotificationService.ReminderOffsets(settings)));
            });
            Test("taskbar highlight respects start window and whole duration", () =>
            {
                var start = new DateTime(2026, 7, 16, 10, 0, 0);
                var item = new CalendarEvent { Start = start, End = start.AddHours(1) };
                var settings = new AppSettings { HighlightStartingMeetings = true, HighlightMinutesBefore = 5, HighlightMinutesAfterStart = 10 };
                Equal(false, WidgetHighlightPolicy.ShouldHighlight(item, settings, start.AddMinutes(-6)));
                Equal(true, WidgetHighlightPolicy.ShouldHighlight(item, settings, start.AddMinutes(-5)));
                Equal(true, WidgetHighlightPolicy.ShouldHighlight(item, settings, start.AddMinutes(9)));
                Equal(false, WidgetHighlightPolicy.ShouldHighlight(item, settings, start.AddMinutes(10)));
                settings.HighlightWholeMeeting = true;
                Equal(true, WidgetHighlightPolicy.ShouldHighlight(item, settings, start.AddMinutes(40)));
            });
            Test("event window hours respect day-boundary toggles", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var clamped = EventWindowPolicy.Range(24, 24, false, false, now);
                Equal(now.Date, clamped.Item1); Equal(now.Date.AddDays(1), clamped.Item2);
                var expanded = EventWindowPolicy.Range(24, 24, true, true, now);
                Equal(now.AddHours(-24), expanded.Item1); Equal(now.AddHours(24), expanded.Item2);
            });
            Test("full-day event range ignores timeline hours", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var range = EventWindowPolicy.FullDayRange(true, true, now);
                Equal(new DateTime(2026, 7, 15), range.Item1); Equal(new DateTime(2026, 7, 18), range.Item2);
            });
            Test("timeline day range supports configurable depth", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var range = EventWindowPolicy.FullDayRange(3, 5, now);
                Equal(new DateTime(2026, 7, 13), range.Item1); Equal(new DateTime(2026, 7, 22), range.Item2);
            });
            Test("calendar event supports 12 and 24 hour clocks", () =>
            {
                var item = new CalendarEvent { Start = new DateTime(2026, 7, 16, 13, 5, 0), End = new DateTime(2026, 7, 16, 14, 0, 0), Use24HourClock = true };
                Equal("13:05", item.StartTimeText);
                item.Use24HourClock = false;
                Equal("1:05 p. m.", item.StartTimeText);
            });
            Test("timeline date supports configured full-date formats", () =>
            {
                var date = new DateTime(2026, 12, 31);
                Equal("31.12.2026", CalendarEvent.FormatDate(date, "dd.MM.yyyy"));
                Equal("2026-12-31", CalendarEvent.FormatDate(date, "yyyy-MM-dd"));
                Equal("31.12.2026", CalendarEvent.FormatDate(date, "invalid"));
            });
            Test("timeline day pages keep today plus configured cross-day events", () =>
            {
                var now = new DateTime(2026, 7, 16, 23, 0, 0);
                var settings = new AppSettings { PastMeetingHours = 2, FutureMeetingHours = 3, IncludePastDay = true, IncludeNextDay = true, ShowFullDayEventList = true };
                var events = new[]
                {
                    new CalendarEvent { Id = "today", Start = now.Date.AddHours(8), End = now.Date.AddHours(9) },
                    new CalendarEvent { Id = "tomorrow-window", Start = now.Date.AddDays(1).AddHours(1), End = now.Date.AddDays(1).AddHours(2) },
                    new CalendarEvent { Id = "tomorrow-late", Start = now.Date.AddDays(1).AddHours(15), End = now.Date.AddDays(1).AddHours(16) }
                };
                var today = EventWindowPolicy.Page(events, settings, 0, now);
                Equal(true, today.Any(item => item.Id == "today")); Equal(true, today.Any(item => item.Id == "tomorrow-window")); Equal(false, today.Any(item => item.Id == "tomorrow-late"));
                var tomorrow = EventWindowPolicy.Page(events, settings, 1, now);
                Equal(2, tomorrow.Count);
            });
            Test("taskbar can ignore free events without hiding popup events", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var events = new[] { new CalendarEvent { Id = "free", Start = now.AddMinutes(5), End = now.AddMinutes(30), IsBusy = false }, new CalendarEvent { Id = "busy", Start = now.AddMinutes(10), End = now.AddMinutes(40), IsBusy = true } };
                Equal("free", EventWindowPolicy.NextTaskbarEvent(events, false, now).Id);
                Equal("busy", EventWindowPolicy.NextTaskbarEvent(events, true, now).Id);
            });
            Test("widget selection skips hidden all-day and ignored free meetings", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var settings = new AppSettings { ShowAllDayEvents = false, IgnoreFreeEventsInTaskbar = true };
                var events = new[]
                {
                    new CalendarEvent { Id = "all-day", Start = now.Date, End = now.Date.AddDays(1), IsAllDay = true, IsBusy = true },
                    new CalendarEvent { Id = "free", Start = now.AddMinutes(-5), End = now.AddMinutes(25), IsBusy = false },
                    new CalendarEvent { Id = "busy", Start = now.AddMinutes(10), End = now.AddMinutes(40), IsBusy = true }
                };
                Equal("busy", EventWindowPolicy.NextWidgetEvent(events, settings, now).Id);
            });
            Test("taskbar-ignored free meetings do not send reminders", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var settings = new AppSettings { IgnoreFreeEventsInTaskbar = true };
                var free = new CalendarEvent { Start = now.AddMinutes(5), End = now.AddMinutes(30), IsBusy = false };
                var busy = new CalendarEvent { Start = now.AddMinutes(10), End = now.AddMinutes(40), IsBusy = true };
                Equal(false, NotificationService.IsNotificationEligible(free, settings, now));
                Equal(true, NotificationService.IsNotificationEligible(busy, settings, now));
                settings.IgnoreFreeEventsInTaskbar = false;
                Equal(true, NotificationService.IsNotificationEligible(free, settings, now));
            });
            Test("taskbar widget applies end-of-day behavior only after today's events", () =>
            {
                var now = new DateTime(2026, 7, 16, 18, 0, 0);
                var today = new CalendarEvent { Id = "today", Start = now.AddHours(1), End = now.AddHours(2) };
                var tomorrow = new CalendarEvent { Id = "tomorrow", Start = now.Date.AddDays(1).AddHours(9), End = now.Date.AddDays(1).AddHours(10) };
                var settings = new AppSettings { EndOfDayWidgetBehavior = EndOfDayWidgetBehavior.NoEventsToday };
                Equal("today", EventWindowPolicy.NextWidgetEvent(new[] { today, tomorrow }, settings, now).Id);
                Equal(null, EventWindowPolicy.NextWidgetEvent(new[] { tomorrow }, settings, now));
                settings.EndOfDayWidgetBehavior = EndOfDayWidgetBehavior.NextEvent;
                Equal("tomorrow", EventWindowPolicy.NextWidgetEvent(new[] { tomorrow }, settings, now).Id);

                var current = new CalendarEvent { Id = "current", Start = now.Date.AddDays(-1).AddHours(23), End = now.AddMinutes(30) };
                settings.EndOfDayWidgetBehavior = EndOfDayWidgetBehavior.Hide;
                Equal("current", EventWindowPolicy.NextWidgetEvent(new[] { current, tomorrow }, settings, now).Id);
            });
            Test("dismissed meetings stay in agendas but are muted elsewhere", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var settings = new AppSettings { ShowFullDayEventList = true };
                var dismissed = new CalendarEvent { Id = "dismissed", Start = now.AddMinutes(5), End = now.AddMinutes(35), IsDismissed = true };
                var active = new CalendarEvent { Id = "active", Start = now.AddMinutes(10), End = now.AddMinutes(40) };
                var events = new[] { dismissed, active };
                Equal(true, EventWindowPolicy.Page(events, settings, 0, now).Any(x => x.Id == "dismissed"));
                Equal("active", EventWindowPolicy.NextTaskbarEvent(events, false, now).Id);
                Equal(false, NotificationService.IsNotificationEligible(dismissed, now));
                Equal(false, EventWindowPolicy.CanDismiss(dismissed, now));
                dismissed.IsDismissed = false;
                Equal("dismissed", EventWindowPolicy.NextTaskbarEvent(events, false, now).Id);
                Equal(true, NotificationService.IsNotificationEligible(dismissed, now));
                var ended = new CalendarEvent { End = now.AddMinutes(-1), IsDismissed = true };
                Equal(false, EventWindowPolicy.CanDismiss(ended, now));
                Equal(true, EventWindowPolicy.ShouldRestoreDismissal(ended, now));
            });
            Test("unknown meeting services use the generic video fallback", () =>
            {
                Equal(true, MeetingServiceIconConverter.IsKnown("Google Meet"));
                Equal(false, MeetingServiceIconConverter.IsKnown("Web meeting"));
            });
            Test("physical locations open in the configured map provider", () =>
            {
                Equal(true, MapLinkService.HasPhysicalLocation("Alexanderplatz 1, Berlin"));
                Equal(false, MapLinkService.HasPhysicalLocation("https://meet.google.com/abc-defg-hij"));
                Contains("google.com/maps", MapLinkService.BuildUrl("Google Maps", "Alexanderplatz 1, Berlin"));
                Contains("openstreetmap.org", MapLinkService.BuildUrl("OpenStreetMap", "Alexanderplatz 1, Berlin"));
            });
            Test("debug events carry provider icon data without calendar writes", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var item = DebugEventFactory.Create("UI test", 5, 30, "Microsoft Teams", "invalid", now);
                Equal("Microsoft Teams", item.MeetingService); Contains("teams.microsoft.com", item.MeetingUrl);
                Equal("#0078D4", item.CalendarColor); Equal("meetingbar-debug", item.CalendarId);
            });
            Console.WriteLine(_failed == 0 ? "All MeetingBar tests passed." : _failed + " test(s) failed.");
            Environment.ExitCode = _failed == 0 ? 0 : 1;
        }

        private static void RenderWidget(string path, bool fallback = false)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

            var app = new MeetingBar.Windows.App(); app.InitializeComponent();
            var settings = new AppSettings { TimelineFutureDays = 1, WidgetWidth = 300, AutoJoin = false };
            var service = (SettingsService)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SettingsService));
            typeof(SettingsService).GetField("<Current>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(service, settings);
            var sync = new CalendarSyncService(service);
            var title = "Project review | Progress, open questions and upcoming deliverables for the team";
            sync.InjectDebugEvent(new CalendarEvent { Id = "layout-test", CalendarId = "meetingbar-debug", Title = title, Start = DateTime.Now.AddDays(16), End = DateTime.Now.AddDays(16).AddHours(1), IsAllDay = fallback });
            Equal(0, sync.Events.Count);
            Equal(1, sync.UpcomingEvents.Count);
            var vm = new MeetingBar.Windows.ViewModels.MainViewModel(sync, service, new MeetingOpener(service));
            Equal(fallback ? "Sin citas próximas" : title, vm.WidgetTitle);
            if (fallback) Contains(title, vm.WidgetSubtitle);
            var window = new TaskbarWidgetWindow(vm, () => { }); window.ShowActivated = false; window.Show(); window.ApplyPosition(settings);
            window.Measure(new System.Windows.Size(window.Width, window.Height));
            window.Arrange(new System.Windows.Rect(0, 0, window.Width, window.Height)); window.UpdateLayout();
            var text = (System.Windows.Controls.TextBlock)window.FindName("WidgetTitle");
            Equal(10.0, text.FontSize);
            if (fallback) Equal(14.0, text.ActualHeight);
            else if (text.ActualHeight <= 15 || text.ActualHeight > 30.1) throw new Exception("Expected two visible title lines; height=" + text.ActualHeight);
            var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            image.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using (var output = System.IO.File.Create(path)) encoder.Save(output);
            window.SetDesiredVisibility(false);
            window.EnsureAboveTaskbar();
            Equal(false, window.IsVisible);
            Equal(false, MeetingBar.Windows.Interop.NativeMethods.IsWindowVisible(new System.Windows.Interop.WindowInteropHelper(window).Handle));
            vm.Dispose(); sync.Dispose(); window.Close();
            Console.WriteLine(fallback ? "PASS no appointments shows a separate all-day subtitle" : "PASS next event survives a one-day agenda; long title renders as two lines at 10pt");
        }

        private static void RenderAgenda(string path)
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            var app = new MeetingBar.Windows.App(); app.InitializeComponent();
            var settings = new AppSettings { AutoJoin = false };
            var service = (SettingsService)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SettingsService));
            typeof(SettingsService).GetField("<Current>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(service, settings);
            var sync = new CalendarSyncService(service);
            var today = DateTime.Today;
            sync.InjectDebugEvent(new CalendarEvent { Id = "day", CalendarId = "meetingbar-debug", Title = "Recordatorio anual del equipo", Start = today, End = today.AddDays(1), IsAllDay = true });
            sync.InjectDebugEvent(new CalendarEvent { Id = "timed", CalendarId = "meetingbar-debug", Title = "Revisión del proyecto", Start = DateTime.Now.AddMinutes(30), End = DateTime.Now.AddMinutes(90) });
            var vm = new MeetingBar.Windows.ViewModels.MainViewModel(sync, service, new MeetingOpener(service));
            typeof(MeetingBar.Windows.ViewModels.MainViewModel).GetField("_isInitialLoading", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(vm, false);
            Equal(2, vm.PageEvents.Count);
            Equal("TODO EL DÍA · FERIADOS Y RECORDATORIOS", vm.PageEvents[0].AgendaSectionHeader);
            Equal("CITAS CON HORA", vm.PageEvents[1].AgendaSectionHeader);
            Equal("timed", vm.WidgetEvent.Id);
            var window = new PopupWindow(vm, () => { }); window.ShowActivated = false; window.Show();
            window.Measure(new System.Windows.Size(window.Width, window.Height)); window.Arrange(new System.Windows.Rect(0, 0, window.Width, window.Height)); window.UpdateLayout();
            var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            image.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using (var output = System.IO.File.Create(path)) encoder.Save(output);
            vm.Dispose(); sync.Dispose(); window.Close();
            Console.WriteLine("PASS agenda visually separates all-day reminders and timed appointments");
        }

        private static void Test(string name, Action action) { try { action(); Console.WriteLine("PASS " + name); } catch (Exception ex) { _failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); } }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("expected " + expected + ", got " + actual); }
        private static void Contains(string expected, string actual) { if (actual == null || !actual.Contains(expected)) throw new Exception("missing " + expected + " in " + actual); }
    }
}
