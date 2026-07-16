using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public sealed class CalendarSyncService : IDisposable
    {
        private readonly SettingsService _settings;
        private readonly OutlookCalendarProvider _outlook = new OutlookCalendarProvider();
        private readonly IcsCalendarProvider _ics = new IcsCalendarProvider();
        private readonly GoogleCalendarProvider _google = new GoogleCalendarProvider();
        private Timer _timer;
        private int _refreshing;
        private List<CalendarEvent> _lastGoodEvents = new List<CalendarEvent>();
        private readonly List<CalendarEvent> _debugEvents = new List<CalendarEvent>();

        public IReadOnlyList<CalendarEvent> Events { get; private set; } = new List<CalendarEvent>();
        public IReadOnlyList<CalendarInfo> Calendars { get; private set; } = new List<CalendarInfo>();
        public ProviderStatus Status { get; private set; } = new ProviderStatus { Provider = "Calendar", Message = "Starting…" };
        public event EventHandler Changed;

        public CalendarSyncService(SettingsService settings) { _settings = settings; }
        public void Start() { Schedule(); _ = RefreshAsync(); }
        public void Schedule()
        {
            _timer?.Dispose();
            _timer = new Timer(async _ => await RefreshAsync(), null, TimeSpan.FromMinutes(Math.Max(1, _settings.Current.RefreshMinutes)), TimeSpan.FromMinutes(Math.Max(1, _settings.Current.RefreshMinutes)));
        }

        public async Task RefreshAsync()
        {
            if (Interlocked.Exchange(ref _refreshing, 1) == 1) return;
            try
            {
                var now = DateTime.Now;
                var range = EventWindowPolicy.Range(_settings.Current, now);
                var from = range.Item1;
                var to = range.Item2;
                Tuple<List<CalendarInfo>, List<CalendarEvent>> result;
                switch (_settings.Current.Provider)
                {
                    case CalendarProviderKind.Ics: result = await _ics.FetchAsync(_settings.Current.IcsSources, from, to); break;
                    case CalendarProviderKind.Google: result = await _google.FetchAsync(_settings.Current, from, to); break;
                    default: result = await _outlook.FetchAsync(from, to); break;
                }
                foreach (var item in result.Item2)
                {
                    var meeting = MeetingLinkDetector.Detect(item, _settings.Current.CustomMeetingRegexes);
                    if (meeting != null) { item.MeetingUrl = meeting.Url; item.MeetingService = meeting.Service; }
                    item.IsDismissed = _settings.Current.DismissedEventIds.Contains(item.Id);
                }
                var activeIds = new HashSet<string>(result.Item2.Where(x => x.End > now).Select(x => x.Id));
                if (_settings.Current.DismissedEventIds.RemoveAll(id => !activeIds.Contains(id)) > 0) _settings.Save();
                Calendars = result.Item1;
                _lastGoodEvents = Filter(result.Item2).ToList();
                Events = Filter(_lastGoodEvents.Concat(_debugEvents)).ToList();
                Status = new ProviderStatus { Provider = _settings.Current.Provider.ToString(), IsHealthy = true, Message = "Updated " + DateTime.Now.ToString("t") };
                AppLog.Info("Calendar refresh succeeded: provider=" + _settings.Current.Provider + ", calendars=" + Calendars.Count + ", visibleEvents=" + Events.Count);
            }
            catch (Exception ex)
            {
                Events = Filter(_lastGoodEvents.Concat(_debugEvents)).ToList();
                Status = new ProviderStatus { Provider = _settings.Current.Provider.ToString(), IsHealthy = false, Message = ex.Message };
                AppLog.Error("Calendar refresh", ex);
            }
            finally { Interlocked.Exchange(ref _refreshing, 0); Changed?.Invoke(this, EventArgs.Empty); }
        }

        private IEnumerable<CalendarEvent> Filter(IEnumerable<CalendarEvent> events)
        {
            var s = _settings.Current;
            var range = EventWindowPolicy.Range(s, DateTime.Now);
            foreach (var item in events.OrderBy(x => x.Start))
            {
                if (item.End <= range.Item1 || item.Start >= range.Item2) continue;
                if (item.IsCancelled || item.IsDismissed) continue;
                if (!s.ShowAllDayEvents && item.IsAllDay) continue;
                if (!s.ShowDeclinedEvents && item.IsDeclined) continue;
                if (!s.ShowTentativeEvents && item.IsTentative) continue;
                if (!s.ShowFreeEvents && !item.IsBusy) continue;
                if (s.RequireMeetingLink && !item.CanJoin) continue;
                if (item.CalendarId != "meetingbar-debug" && s.SelectedCalendarIds.Count > 0 && !s.SelectedCalendarIds.Contains(item.CalendarId)) continue;
                if (s.HiddenTitlePatterns.Any(p => !string.IsNullOrWhiteSpace(p) && System.Text.RegularExpressions.Regex.IsMatch(item.Title ?? "", p, System.Text.RegularExpressions.RegexOptions.IgnoreCase))) continue;
                yield return item;
            }
        }

        public CalendarEvent LatestDebugEvent => _debugEvents.LastOrDefault();
        public void InjectDebugEvent(CalendarEvent item)
        {
            if (item == null) return;
            _debugEvents.Add(item);
            Events = Filter(_lastGoodEvents.Concat(_debugEvents)).ToList();
            Changed?.Invoke(this, EventArgs.Empty);
        }
        public void ClearDebugEvents()
        {
            _debugEvents.Clear();
            Events = Filter(_lastGoodEvents).ToList();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public GoogleCalendarProvider Google => _google;
        public void Dispose() => _timer?.Dispose();
    }
}
