using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.Services;

namespace MeetingBar.Windows.ViewModels
{
    public sealed class MainViewModel : BindableBase, IDisposable
    {
        private readonly CalendarSyncService _sync;
        private readonly SettingsService _settings;
        private readonly MeetingOpener _opener;
        private readonly DispatcherTimer _clock;
        private bool _isRefreshing;
        private bool _isInitialLoading = true;
        private string _statusText;
        private int _selectedDayOffset;
        public ObservableCollection<CalendarEvent> Events { get; } = new ObservableCollection<CalendarEvent>();
        public ObservableCollection<CalendarEvent> PageEvents { get; } = new ObservableCollection<CalendarEvent>();
        public ObservableCollection<CalendarEvent> TimelineEvents { get; } = new ObservableCollection<CalendarEvent>();
        public ObservableCollection<Bookmark> Bookmarks { get; } = new ObservableCollection<Bookmark>();
        public CalendarEvent TaskbarEvent => WidgetEvent;
        public CalendarEvent WidgetEvent => EventWindowPolicy.NextWidgetEvent(_sync.UpcomingEvents, _settings.Current, DateTime.Now);
        public CalendarEvent NextEvent => WidgetEvent;
        public CalendarEvent NextAllDayEvent => _sync.UpcomingEvents.FirstOrDefault(item => item.IsAllDay && !item.IsDismissed && item.End > DateTime.Now);
        public string WidgetSubtitle => WidgetEvent == null && _settings.Current.EndOfDayWidgetBehavior == EndOfDayWidgetBehavior.NextEvent && NextAllDayEvent != null
            ? EventWindowPolicy.WidgetWhen(NextAllDayEvent, DateTime.Now, _settings.Current.Use24HourClock) + " · " + NextAllDayEvent.Title : "";
        public bool HasWidgetSubtitle => !string.IsNullOrEmpty(WidgetSubtitle);
        public bool ShouldShowTaskbarWidget => _settings.Current.EndOfDayWidgetBehavior != EndOfDayWidgetBehavior.Hide || WidgetEvent != null;
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
        public bool IsRefreshing { get => _isRefreshing; set => Set(ref _isRefreshing, value); }
        public Visibility LoadingVisibility => _isInitialLoading ? Visibility.Visible : Visibility.Collapsed;
        public bool HasEvents => PageEvents.Count > 0;
        public bool HasBookmarks => Bookmarks.Count > 0;
        public int TimelinePastHours => _settings.Current.PastMeetingHours;
        public int TimelineFutureHours => _settings.Current.FutureMeetingHours;
        public bool TimelineIncludesPastDay => _settings.Current.IncludePastDay;
        public bool TimelineIncludesNextDay => _settings.Current.IncludeNextDay;
        public int TimelinePastDays => _settings.Current.TimelinePastDays;
        public int TimelineFutureDays => _settings.Current.TimelineFutureDays;
        public bool Use24HourClock => _settings.Current.Use24HourClock;
        public bool ShowTimelineDate => _settings.Current.ShowTimelineDate;
        public string TimelineDateFormat => _settings.Current.TimelineDateFormat;
        public string Header => NextEvent?.IsAllDay == true ? "TODO EL DÍA" : NextEvent?.IsCurrent == true ? "EN CURSO" : "PRÓXIMA CITA";
        public string NextTitle => NextEvent?.Title ?? "Sin citas próximas";
        public string NextMeta => NextEvent == null ? (HasWidgetSubtitle ? WidgetSubtitle : StatusText) : EventWindowPolicy.WidgetTiming(NextEvent, DateTime.Now, _settings.Current.Use24HourClock);
        public string WidgetTitle
        {
            get
            {
                if (WidgetEvent == null)
                {
                    switch (_settings.Current.EndOfDayWidgetBehavior)
                    {
                        case EndOfDayWidgetBehavior.NoEventsToday: return "No events for today";
                        case EndOfDayWidgetBehavior.CustomMessage: return TruncateWidgetTitle(string.IsNullOrWhiteSpace(_settings.Current.EndOfDayCustomMessage) ? "Done for today" : _settings.Current.EndOfDayCustomMessage.Trim());
                        default: return "Sin citas próximas";
                    }
                }
                var mode = _settings.Current.TitleMode;
                if (mode == TitleMode.Hidden) return "";
                if (mode == TitleMode.Dot) return "•";
                if (mode == TitleMode.Generic) return "Meeting";
                return WidgetEvent.Title ?? "Untitled event";
            }
        }
        public string WidgetTime => WidgetEvent == null || _settings.Current.TimeMode == TimeMode.Hidden ? "" : _settings.Current.TimeMode == TimeMode.DateAndTime
            ? EventWindowPolicy.WidgetTiming(WidgetEvent, DateTime.Now, _settings.Current.Use24HourClock)
            : WidgetEvent.IsCurrent && _settings.Current.ShowOngoingTimeRemaining ? WidgetEvent.Remaining : _settings.Current.TimeMode == TimeMode.StartTime ? CalendarEvent.FormatTime(WidgetEvent.Start, _settings.Current.Use24HourClock) : WidgetEvent.Countdown;
        public Visibility WidgetIconVisibility => WidgetEvent != null && _settings.Current.ShowServiceIcon ? Visibility.Visible : Visibility.Collapsed;
        public string WidgetEventColor => WidgetEvent?.CalendarColor ?? "#6D7CFF";
        public bool CanJoinNext => NextEvent?.CanJoin == true;
        public ICommand JoinCommand { get; }
        public ICommand OpenEventCommand { get; }
        public ICommand OpenMapCommand { get; }
        public ICommand CopyMeetingLinkCommand { get; }
        public ICommand DismissEventCommand { get; }
        public ICommand RestoreEventCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateMeetingCommand { get; }
        public ICommand OpenCalendarCommand { get; }
        public ICommand OpenBookmarkCommand { get; }

        public MainViewModel(CalendarSyncService sync, SettingsService settings, MeetingOpener opener)
        {
            _sync = sync; _settings = settings; _opener = opener;
            JoinCommand = new RelayCommand(x => _opener.Open(x as CalendarEvent ?? NextEvent));
            OpenEventCommand = new RelayCommand(OpenEventInCalendar, CanOpenEventInCalendar);
            OpenMapCommand = new RelayCommand(x => { var item = x as CalendarEvent; _opener.Open(MapLinkService.BuildUrl(_settings.Current.MapProvider, item?.Location)); }, x => MapLinkService.HasPhysicalLocation((x as CalendarEvent)?.Location));
            CopyMeetingLinkCommand = new RelayCommand(x => { var item = x as CalendarEvent; if (item?.CanJoin == true) try { Clipboard.SetText(item.MeetingUrl); } catch { } }, x => (x as CalendarEvent)?.CanJoin == true);
            DismissEventCommand = new RelayCommand(x => _sync.DismissEvent(x as CalendarEvent), x => EventWindowPolicy.CanDismiss(x as CalendarEvent, DateTime.Now));
            RestoreEventCommand = new RelayCommand(x => _sync.RestoreEvent(x as CalendarEvent), x => (x as CalendarEvent)?.CanRestoreNotifications == true);
            RefreshCommand = new RelayCommand(async () => await Refresh());
            CreateMeetingCommand = new RelayCommand(() => _opener.Open(_settings.Current.CreateMeetingUrl));
            OpenCalendarCommand = new RelayCommand(OpenCalendar, () => CanOpenCalendar);
            OpenBookmarkCommand = new RelayCommand(x => _opener.Open((x as Bookmark)?.Url));
            _sync.Changed += SyncChanged;
            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
            _clock.Tick += (s, e) => { _sync.RestoreExpiredDismissals(DateTime.Now); foreach (var item in TimelineEvents) item.RefreshComputed(); RaiseComputed(); };
            _clock.Start(); Rebuild();
        }
        public bool CanOpenCalendar => _settings.Current.Provider != CalendarProviderKind.Ics || !string.IsNullOrWhiteSpace(NextEvent?.CalendarOpenUrl);
        private void OpenCalendar()
        {
            switch (_settings.Current.Provider)
            {
                case CalendarProviderKind.Google: _opener.Open("https://calendar.google.com/calendar/u/0/r"); break;
                case CalendarProviderKind.Outlook:
                    try { _opener.Open("outlookcal:"); }
                    catch { _opener.Open("https://outlook.office.com/calendar/"); }
                    break;
                default: _opener.Open(NextEvent?.CalendarOpenUrl); break;
            }
        }

        private bool CanOpenEventInCalendar(object value)
        {
            var item = value as CalendarEvent;
            return item != null && (!string.IsNullOrWhiteSpace(item.CalendarOpenUrl) || !string.IsNullOrWhiteSpace(item.EventUrl) || _settings.Current.Provider != CalendarProviderKind.Ics);
        }

        private void OpenEventInCalendar(object value)
        {
            var item = value as CalendarEvent;
            if (item == null) return;
            var url = item.CalendarOpenUrl ?? item.EventUrl;
            if (!string.IsNullOrWhiteSpace(url)) { _opener.Open(url); return; }
            if (_settings.Current.Provider == CalendarProviderKind.Outlook) _opener.Open("outlookcal:");
            else if (_settings.Current.Provider == CalendarProviderKind.Google) _opener.Open("https://calendar.google.com/calendar/u/0/r");
        }

        private async Task Refresh() { IsRefreshing = true; await _sync.RefreshAsync(); IsRefreshing = false; }
        private void SyncChanged(object sender, EventArgs e) => Application.Current.Dispatcher.BeginInvoke(new Action(() => { _isInitialLoading = false; Rebuild(); }));
        private void Rebuild()
        {
            Events.Clear(); foreach (var item in _sync.Events) Events.Add(item);
            TimelineEvents.Clear(); foreach (var item in _sync.TimelineEvents) TimelineEvents.Add(item);
            SelectDay(_selectedDayOffset);
            Bookmarks.Clear(); foreach (var item in _settings.Current.Bookmarks) Bookmarks.Add(item);
            StatusText = _sync.Status.Message; RaiseComputed();
        }
        public void SelectDay(int dayOffset)
        {
            var minimum = _settings.Current.IncludePastDay ? -Math.Max(0, Math.Min(30, _settings.Current.TimelinePastDays)) : 0;
            var maximum = _settings.Current.IncludeNextDay ? Math.Max(0, Math.Min(30, _settings.Current.TimelineFutureDays)) : 0;
            _selectedDayOffset = Math.Max(minimum, Math.Min(maximum, dayOffset));
            PageEvents.Clear();
            DateTime? previousDay = null;
            bool? previousAllDay = null;
            foreach (var item in EventWindowPolicy.GroupAgenda(EventWindowPolicy.Page(TimelineEvents, _settings.Current, _selectedDayOffset, DateTime.Now)))
            {
                item.ShowDayDivider = previousDay.HasValue && previousDay.Value != item.Start.Date;
                item.AgendaSectionHeader = previousDay != item.Start.Date || previousAllDay != item.IsAllDay ? (item.IsAllDay ? "TODO EL DÍA · FERIADOS Y RECORDATORIOS" : "CITAS CON HORA") : "";
                previousDay = item.Start.Date;
                previousAllDay = item.IsAllDay;
                PageEvents.Add(item);
            }
            Raise(nameof(HasEvents));
        }
        private string TruncateWidgetTitle(string title)
        {
            var max = Math.Max(5, _settings.Current.TitleMaxLength);
            return title.Length > max ? title.Substring(0, max - 1) + "…" : title;
        }
        private void RaiseComputed() { Raise(nameof(NextAllDayEvent)); Raise(nameof(WidgetSubtitle)); Raise(nameof(HasWidgetSubtitle)); Raise(nameof(NextEvent)); Raise(nameof(TaskbarEvent)); Raise(nameof(WidgetEvent)); Raise(nameof(ShouldShowTaskbarWidget)); Raise(nameof(Header)); Raise(nameof(NextTitle)); Raise(nameof(NextMeta)); Raise(nameof(WidgetTitle)); Raise(nameof(WidgetTime)); Raise(nameof(WidgetIconVisibility)); Raise(nameof(WidgetEventColor)); Raise(nameof(CanJoinNext)); Raise(nameof(CanOpenCalendar)); Raise(nameof(HasEvents)); Raise(nameof(HasBookmarks)); Raise(nameof(TimelinePastHours)); Raise(nameof(TimelineFutureHours)); Raise(nameof(TimelineIncludesPastDay)); Raise(nameof(TimelineIncludesNextDay)); Raise(nameof(TimelinePastDays)); Raise(nameof(TimelineFutureDays)); Raise(nameof(Use24HourClock)); Raise(nameof(ShowTimelineDate)); Raise(nameof(TimelineDateFormat)); Raise(nameof(LoadingVisibility)); }
        public void RefreshSettings() { RaiseComputed(); }
        public void Dispose() { _sync.Changed -= SyncChanged; _clock.Stop(); }
    }
}
