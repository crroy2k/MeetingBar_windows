using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.Services;

namespace MeetingBar.Windows.ViewModels
{
    public sealed class SettingsViewModel : BindableBase
    {
        private readonly SettingsService _service;
        private readonly CalendarSyncService _sync;
        private readonly Action _saved;
        private string _connectionStatus;
        public AppSettings Settings { get; }
        public Array WidgetModes => Enum.GetValues(typeof(WidgetMode));
        public Array Providers => Enum.GetValues(typeof(CalendarProviderKind));
        public Array TitleModes => Enum.GetValues(typeof(TitleMode));
        public Array TimeModes => Enum.GetValues(typeof(TimeMode));
        public ObservableCollection<CalendarInfo> Calendars { get; } = new ObservableCollection<CalendarInfo>();
        public ObservableCollection<Bookmark> Bookmarks { get; } = new ObservableCollection<Bookmark>();
        public string ConnectionStatus { get => _connectionStatus; set => Set(ref _connectionStatus, value); }
        public ICommand SaveCommand { get; }
        public ICommand ConnectGoogleCommand { get; }
        public ICommand DisconnectGoogleCommand { get; }
        public ICommand AddBookmarkCommand { get; }
        public ICommand RemoveBookmarkCommand { get; }

        public SettingsViewModel(SettingsService service, CalendarSyncService sync, Action saved)
        {
            _service = service; _sync = sync; _saved = saved; Settings = service.CopyCurrent();
            foreach (var x in sync.Calendars) { x.Selected = Settings.SelectedCalendarIds.Count == 0 || Settings.SelectedCalendarIds.Contains(x.Id); Calendars.Add(x); }
            foreach (var x in Settings.Bookmarks) Bookmarks.Add(x);
            ConnectionStatus = sync.Google.IsConnected(Settings) ? "Connected as " + Settings.GoogleAccountEmail : "Not connected";
            SaveCommand = new RelayCommand<Window>(Save);
            ConnectGoogleCommand = new RelayCommand(async () => await ConnectGoogle());
            DisconnectGoogleCommand = new RelayCommand(DisconnectGoogle);
            AddBookmarkCommand = new RelayCommand(() => Bookmarks.Add(new Bookmark { Name = "New meeting", Url = "https://" }));
            RemoveBookmarkCommand = new RelayCommand(x => { if (x is Bookmark b) Bookmarks.Remove(b); });
        }

        private void Save(Window window)
        {
            Settings.SelectedCalendarIds = Calendars.Where(x => x.Selected).Select(x => x.Id).ToList();
            Settings.Bookmarks = Bookmarks.Where(x => !string.IsNullOrWhiteSpace(x.Name) && Uri.IsWellFormedUriString(x.Url, UriKind.Absolute)).ToList();
            StartupService.SetEnabled(Settings.StartWithWindows); _service.Replace(Settings); _saved(); window?.Close();
        }
        private async Task ConnectGoogle()
        {
            try { ConnectionStatus = "Waiting for browser…"; await _sync.Google.ConnectAsync(Settings); ConnectionStatus = "Connected as " + Settings.GoogleAccountEmail; }
            catch (Exception ex) { ConnectionStatus = ex.Message; }
        }
        private void DisconnectGoogle() { _sync.Google.Disconnect(Settings); ConnectionStatus = "Not connected"; }
    }

    public sealed class RelayCommand<T> : ICommand where T : class
    {
        private readonly Action<T> _execute; public RelayCommand(Action<T> execute) { _execute = execute; }
        public bool CanExecute(object parameter) => true; public void Execute(object parameter) => _execute(parameter as T);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }
}
