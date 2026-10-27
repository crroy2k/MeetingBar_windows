using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public sealed class OutlookCalendarProvider
    {
        public Task<Tuple<List<CalendarInfo>, List<CalendarEvent>>> FetchAsync(DateTime from, DateTime to)
        {
            return Task.Run(() => Fetch(from, to));
        }

        private Tuple<List<CalendarInfo>, List<CalendarEvent>> Fetch(DateTime from, DateTime to)
        {
            var calendars = new List<CalendarInfo>();
            var events = new List<CalendarEvent>();
            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null) throw new InvalidOperationException("Classic Outlook desktop is not installed. Choose ICS or Google Calendar in Settings.");
            object app = null, session = null, stores = null;
            try
            {
                app = Activator.CreateInstance(outlookType);
                session = Invoke(app, "GetNamespace", "MAPI");
                stores = Get(session, "Stores");
                var storeCount = Convert.ToInt32(Get(stores, "Count"), CultureInfo.InvariantCulture);
                for (var s = 1; s <= storeCount; s++)
                {
                    object store = null, root = null;
                    try
                    {
                        store = GetIndexed(stores, s);
                        root = Invoke(store, "GetRootFolder");
                        ReadFolderTree(root, from, to, calendars, events);
                    }
                    catch (Exception error) { throw new InvalidOperationException("Could not read an Outlook calendar store. Keeping previous events.", error); }
                    finally { Release(root); Release(store); }
                }
            }
            finally { Release(stores); Release(session); Release(app); }
            return Tuple.Create(calendars.GroupBy(x => x.Id).Select(x => x.First()).ToList(), events.OrderBy(x => x.Start).ToList());
        }

        private void ReadFolderTree(object folder, DateTime from, DateTime to, List<CalendarInfo> calendars, List<CalendarEvent> output)
        {
            if (folder == null) return;
            var defaultItemType = SafeInt(() => Get(folder, "DefaultItemType"), -1);
            if (defaultItemType == 1) ReadCalendarFolder(folder, from, to, calendars, output); // olAppointmentItem
            object folders = null;
            try
            {
                folders = Get(folder, "Folders");
                var count = Convert.ToInt32(Get(folders, "Count"), CultureInfo.InvariantCulture);
                for (var i = 1; i <= count; i++)
                {
                    object child = null;
                    try { child = GetIndexed(folders, i); ReadFolderTree(child, from, to, calendars, output); }
                    catch (Exception error) { throw new InvalidOperationException("Could not read an Outlook calendar folder. Keeping previous events.", error); }
                    finally { Release(child); }
                }
            }
            finally { Release(folders); }
        }

        private void ReadCalendarFolder(object folder, DateTime from, DateTime to, List<CalendarInfo> calendars, List<CalendarEvent> output)
        {
            var id = SafeString(() => Get(folder, "EntryID")) ?? Guid.NewGuid().ToString("N");
            var name = SafeString(() => Get(folder, "Name")) ?? "Outlook Calendar";
            var color = ColorFor(id);
            calendars.Add(new CalendarInfo { Id = id, Name = name, Source = "Outlook", Color = color });
            object items = null, restricted = null;
            try
            {
                items = Get(folder, "Items");
                Invoke(items, "Sort", "[Start]");
                Set(items, "IncludeRecurrences", true);
                var filter = "[Start] < '" + to.ToString("g", CultureInfo.CurrentCulture) + "' AND [End] > '" + from.ToString("g", CultureInfo.CurrentCulture) + "'";
                restricted = Invoke(items, "Restrict", filter);
                // Count is undefined for some recurring Outlook collections.
                for (var i = 1; i <= 5001; i++)
                {
                    object item = null;
                    try
                    {
                        item = Invoke(restricted, i == 1 ? "GetFirst" : "GetNext");
                        if (item == null) break;
                        if (i > 5000) throw new InvalidOperationException("Outlook result limit reached; narrow the calendar selection.");
                        var start = Convert.ToDateTime(Get(item, "Start"), CultureInfo.InvariantCulture);
                        var end = Convert.ToDateTime(Get(item, "End"), CultureInfo.InvariantCulture);
                        if (start == DateTime.MinValue || end <= from || start >= to) continue;
                        var response = SafeInt(() => Get(item, "ResponseStatus"), 0);
                        var busy = SafeInt(() => Get(item, "BusyStatus"), 2);
                        var body = SafeString(() => Get(item, "Body"));
                        var location = SafeString(() => Get(item, "Location"));
                        var itemUrl = TryGetOnlineMeetingUrl(item);
                        output.Add(new CalendarEvent
                        {
                            Id = OccurrenceId(SafeString(() => Get(item, "GlobalAppointmentID")) ?? SafeString(() => Get(item, "EntryID")) ?? Guid.NewGuid().ToString("N"), start),
                            Title = SafeString(() => Get(item, "Subject")) ?? "Untitled event", Notes = body, Location = location,
                            ConferenceUrl = itemUrl, CalendarId = id, CalendarName = name, CalendarColor = color,
                            Organizer = SafeString(() => Get(item, "Organizer")), Start = start, End = end,
                            IsAllDay = SafeBool(() => Get(item, "AllDayEvent")), IsDeclined = response == 4,
                            IsTentative = response == 2, IsCancelled = SafeInt(() => Get(item, "MeetingStatus"), 0) == 5,
                            IsBusy = busy != 0
                        });
                    }
                    catch (Exception error) { throw new InvalidOperationException("Could not read an Outlook appointment. Keeping previous events.", error); }
                    finally { Release(item); }
                }
            }
            finally { Release(restricted); Release(items); }
        }

        internal static string OccurrenceId(string seriesId, DateTime start) => seriesId + ":" + start.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);

        private static string TryGetOnlineMeetingUrl(object item)
        {
            try
            {
                var accessor = Get(item, "PropertyAccessor");
                try { return Convert.ToString(Invoke(accessor, "GetProperty", "http://schemas.microsoft.com/mapi/string/{00020329-0000-0000-C000-000000000046}/OnlineMeetingExternalLink")); }
                finally { Release(accessor); }
            }
            catch { return null; }
        }

        private static object Get(object o, string name) => o.GetType().InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, o, null);
        private static object GetIndexed(object o, int i) => o.GetType().InvokeMember("Item", System.Reflection.BindingFlags.GetProperty, null, o, new object[] { i });
        private static object Invoke(object o, string name, params object[] args) => o.GetType().InvokeMember(name, System.Reflection.BindingFlags.InvokeMethod, null, o, args);
        private static void Set(object o, string name, object value) => o.GetType().InvokeMember(name, System.Reflection.BindingFlags.SetProperty, null, o, new[] { value });
        private static void Release(object o) { if (o != null && Marshal.IsComObject(o)) try { Marshal.FinalReleaseComObject(o); } catch { } }
        private static int SafeInt(Func<object> f, int fallback) { try { return Convert.ToInt32(f(), CultureInfo.InvariantCulture); } catch { return fallback; } }
        private static string SafeString(Func<object> f) { try { return Convert.ToString(f(), CultureInfo.InvariantCulture); } catch { return null; } }
        private static DateTime SafeDate(Func<object> f) { try { return Convert.ToDateTime(f(), CultureInfo.CurrentCulture); } catch { return DateTime.MinValue; } }
        private static bool SafeBool(Func<object> f) { try { return Convert.ToBoolean(f(), CultureInfo.InvariantCulture); } catch { return false; } }
        private static string ColorFor(string id) { var colors = new[] { "#6D7CFF", "#49C5B6", "#FC8C6A", "#C66BFF", "#F5C451" }; return colors[Math.Abs(id.GetHashCode()) % colors.Length]; }
    }
}
