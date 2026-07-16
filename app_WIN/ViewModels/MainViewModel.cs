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
        private string _statusText;
        public ObservableCollection<CalendarEvent> Events { get; } = new ObservableCollection<CalendarEvent>();
        public ObservableCollection<Bookmark> Bookmarks { get; } = new ObservableCollection<Bookmark>();
        public CalendarEvent NextEvent => Events.FirstOrDefault(x => x.End > DateTime.Now);
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
        public bool IsRefreshing { get => _isRefreshing; set => Set(ref _isRefreshing, value); }
        public bool HasEvents => Events.Count > 0;
        public bool HasBookmarks => Bookmarks.Count > 0;
        public string Header => NextEvent?.IsCurrent == true ? "CURRENT MEETING" : "NEXT MEETING";
        public string NextTitle => NextEvent?.Title ?? "No upcoming meetings";
        public string NextMeta => NextEvent == null ? StatusText : NextEvent.TimeRange + "  •  " + NextEvent.Countdown;
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
        public string WidgetTime => NextEvent == null || _settings.Current.TimeMode == TimeMode.Hidden ? "" : _settings.Current.TimeMode == TimeMode.StartTime ? NextEvent.Start.ToString("t") : NextEvent.Countdown;
        public string WidgetGlyph
        {
            get
            {
                if (!_settings.Current.ShowServiceIcon || NextEvent == null) return "▣";
                switch (NextEvent.MeetingService)
                {
                    case "Zoom": return "Z";
                    case "Microsoft Teams": return "T";
                    case "Google Meet": return "M";
                    case "Cisco Webex": return "W";
                    case "Slack Huddle": return "S";
                    default: return "↗";
                }
            }
        }
        public bool CanJoinNext => NextEvent?.CanJoin == true;
        public ICommand JoinCommand { get; }
        public ICommand DismissCommand { get; }
        public ICommand OpenEventCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand CreateMeetingCommand { get; }
        public ICommand OpenBookmarkCommand { get; }

        public MainViewModel(CalendarSyncService sync, SettingsService settings, MeetingOpener opener)
        {
            _sync = sync; _settings = settings; _opener = opener;
            JoinCommand = new RelayCommand(x => _opener.Open(x as CalendarEvent ?? NextEvent));
            OpenEventCommand = new RelayCommand(x => { var item = x as CalendarEvent; var url = item?.CalendarOpenUrl ?? item?.EventUrl; if (!string.IsNullOrWhiteSpace(url)) _opener.Open(url); });
            DismissCommand = new RelayCommand(x => Dismiss(x as CalendarEvent));
            RefreshCommand = new RelayCommand(async () => await Refresh());
            CreateMeetingCommand = new RelayCommand(() => _opener.Open(_settings.Current.CreateMeetingUrl));
            OpenBookmarkCommand = new RelayCommand(x => _opener.Open((x as Bookmark)?.Url));
            _sync.Changed += SyncChanged;
            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
            _clock.Tick += (s, e) => { foreach (var item in Events) item.RefreshComputed(); RaiseComputed(); };
            _clock.Start(); Rebuild();
        }

        private async Task Refresh() { IsRefreshing = true; await _sync.RefreshAsync(); IsRefreshing = false; }
        private void SyncChanged(object sender, EventArgs e) => Application.Current.Dispatcher.BeginInvoke(new Action(Rebuild));
        private void Rebuild()
        {
            Events.Clear(); foreach (var item in _sync.Events) Events.Add(item);
            Bookmarks.Clear(); foreach (var item in _settings.Current.Bookmarks) Bookmarks.Add(item);
            StatusText = _sync.Status.Message; RaiseComputed();
        }
        private void RaiseComputed() { Raise(nameof(NextEvent)); Raise(nameof(Header)); Raise(nameof(NextTitle)); Raise(nameof(NextMeta)); Raise(nameof(WidgetTitle)); Raise(nameof(WidgetTime)); Raise(nameof(WidgetGlyph)); Raise(nameof(CanJoinNext)); Raise(nameof(HasEvents)); Raise(nameof(HasBookmarks)); }
        private void Dismiss(CalendarEvent item)
        {
            if (item == null) return; if (!_settings.Current.DismissedEventIds.Contains(item.Id)) _settings.Current.DismissedEventIds.Add(item.Id); _settings.Save(); Events.Remove(item); RaiseComputed();
        }
        public void Dispose() { _sync.Changed -= SyncChanged; _clock.Stop(); }
    }
}
