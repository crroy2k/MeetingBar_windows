using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        public ObservableCollection<CalendarEvent> Events { get; } = new ObservableCollection<CalendarEvent>();
        public ObservableCollection<Bookmark> Bookmarks { get; } = new ObservableCollection<Bookmark>();
        public CalendarEvent NextEvent => Events.FirstOrDefault(x => x.End > DateTime.Now);
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
        public bool IsRefreshing { get => _isRefreshing; set => Set(ref _isRefreshing, value); }
        public Visibility LoadingVisibility => _isInitialLoading ? Visibility.Visible : Visibility.Collapsed;
        public bool HasEvents => Events.Count > 0;
        public bool HasBookmarks => Bookmarks.Count > 0;
        public string Header => NextEvent?.IsCurrent == true ? "CURRENT MEETING" : "NEXT MEETING";
        public string NextTitle => NextEvent?.Title ?? "No upcoming meetings";
        public string NextMeta => NextEvent == null ? StatusText : NextEvent.TimeRange + "  •  " + (NextEvent.IsCurrent && _settings.Current.ShowOngoingTimeRemaining ? NextEvent.Remaining : NextEvent.Countdown);
        public string WidgetTitle
        {
            get
            {
                if (NextEvent == null) return "No upcoming meetings";
                var mode = _settings.Current.TitleMode;
                if (mode == TitleMode.Hidden) return "";
                if (mode == TitleMode.Dot) return "•";
                if (mode == TitleMode.Generic) return "Meeting";
                var title = NextEvent.Title ?? "Untitled event";
                var max = Math.Max(5, _settings.Current.TitleMaxLength);
                return title.Length > max ? title.Substring(0, max - 1) + "…" : title;
            }
        }
        public string WidgetTime => NextEvent == null || _settings.Current.TimeMode == TimeMode.Hidden ? "" : NextEvent.IsCurrent && _settings.Current.ShowOngoingTimeRemaining ? NextEvent.Remaining : _settings.Current.TimeMode == TimeMode.StartTime ? NextEvent.Start.ToString("t") : NextEvent.Countdown;
        public Visibility WidgetIconVisibility => _settings.Current.ShowServiceIcon ? Visibility.Visible : Visibility.Collapsed;
        public string WidgetEventColor => NextEvent?.CalendarColor ?? "#6D7CFF";
        public bool CanJoinNext => NextEvent?.CanJoin == true;
        public ICommand JoinCommand { get; }
        public ICommand OpenEventCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateMeetingCommand { get; }
        public ICommand OpenCalendarCommand { get; }
        public ICommand OpenBookmarkCommand { get; }

        public MainViewModel(CalendarSyncService sync, SettingsService settings, MeetingOpener opener)
        {
            _sync = sync; _settings = settings; _opener = opener;
            JoinCommand = new RelayCommand(x => _opener.Open(x as CalendarEvent ?? NextEvent));
            OpenEventCommand = new RelayCommand(x => { var item = x as CalendarEvent; var url = item?.CalendarOpenUrl ?? item?.EventUrl; if (!string.IsNullOrWhiteSpace(url)) _opener.Open(url); });
            RefreshCommand = new RelayCommand(async () => await Refresh());
            CreateMeetingCommand = new RelayCommand(() => _opener.Open(_settings.Current.CreateMeetingUrl));
            OpenCalendarCommand = new RelayCommand(OpenCalendar, () => CanOpenCalendar);
            OpenBookmarkCommand = new RelayCommand(x => _opener.Open((x as Bookmark)?.Url));
            _sync.Changed += SyncChanged;
            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
            _clock.Tick += (s, e) => { foreach (var item in Events) item.RefreshComputed(); RaiseComputed(); };
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

        private async Task Refresh() { IsRefreshing = true; await _sync.RefreshAsync(); IsRefreshing = false; }
        private void SyncChanged(object sender, EventArgs e) => Application.Current.Dispatcher.BeginInvoke(new Action(() => { _isInitialLoading = false; Rebuild(); }));
        private void Rebuild()
        {
            Events.Clear(); foreach (var item in _sync.Events) Events.Add(item);
            Bookmarks.Clear(); foreach (var item in _settings.Current.Bookmarks) Bookmarks.Add(item);
            StatusText = _sync.Status.Message; RaiseComputed();
        }
        private void RaiseComputed() { Raise(nameof(NextEvent)); Raise(nameof(Header)); Raise(nameof(NextTitle)); Raise(nameof(NextMeta)); Raise(nameof(WidgetTitle)); Raise(nameof(WidgetTime)); Raise(nameof(WidgetIconVisibility)); Raise(nameof(WidgetEventColor)); Raise(nameof(CanJoinNext)); Raise(nameof(CanOpenCalendar)); Raise(nameof(HasEvents)); Raise(nameof(HasBookmarks)); Raise(nameof(LoadingVisibility)); }
        public void RefreshSettings() { _isInitialLoading = true; RaiseComputed(); }
        public void Dispose() { _sync.Changed -= SyncChanged; _clock.Stop(); }
    }
}
