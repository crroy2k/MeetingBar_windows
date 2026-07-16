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
        private static void Main()
        {
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
            Test("defaults retain privacy-safe local provider", () => Equal(CalendarProviderKind.Outlook, new AppSettings().Provider));
            Test("requires Google Calendar read-only scope", () => { Equal(true, GoogleCalendarProvider.HasCalendarScope("openid https://www.googleapis.com/auth/calendar.readonly")); Equal(false, GoogleCalendarProvider.HasCalendarScope("openid https://www.googleapis.com/auth/userinfo.email")); });
            Test("explains disabled Google Calendar API", () => Contains("not enabled", GoogleCalendarProvider.ApiErrorMessage(System.Net.HttpStatusCode.Forbidden, "{\"error\":{\"errors\":[{\"reason\":\"accessNotConfigured\"}]}}")));
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
                Contains("left", current.Remaining); Contains("Ended", past.Countdown);
            });
            Test("event window hours respect day-boundary toggles", () =>
            {
                var now = new DateTime(2026, 7, 16, 10, 0, 0);
                var clamped = EventWindowPolicy.Range(24, 24, false, false, now);
                Equal(now.Date, clamped.Item1); Equal(now.Date.AddDays(1), clamped.Item2);
                var expanded = EventWindowPolicy.Range(24, 24, true, true, now);
                Equal(now.AddHours(-24), expanded.Item1); Equal(now.AddHours(24), expanded.Item2);
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

        private static void Test(string name, Action action) { try { action(); Console.WriteLine("PASS " + name); } catch (Exception ex) { _failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); } }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("expected " + expected + ", got " + actual); }
        private static void Contains(string expected, string actual) { if (actual == null || !actual.Contains(expected)) throw new Exception("missing " + expected + " in " + actual); }
    }
}
