using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using MeetingBar.Windows.Models;
using MeetingBar.Windows.Views;
using Forms = System.Windows.Forms;

namespace MeetingBar.Windows.Services
{
    public sealed class NotificationService : IDisposable
    {
        private readonly SettingsService _settings;
        private readonly MeetingOpener _opener;
        private readonly Forms.NotifyIcon _tray;
        private readonly DispatcherTimer _timer;
        private readonly HashSet<string> _sent = new HashSet<string>();
        private Func<IReadOnlyList<CalendarEvent>> _events;

        public NotificationService(SettingsService settings, MeetingOpener opener, Forms.NotifyIcon tray)
        {
            _settings = settings; _opener = opener; _tray = tray;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
            _timer.Tick += (s, e) => Check();
        }

        public void Start(Func<IReadOnlyList<CalendarEvent>> events) { _events = events; _timer.Start(); }
        private void Check()
        {
            var now = DateTime.Now; var settings = _settings.Current;
            foreach (var item in (_events?.Invoke() ?? new List<CalendarEvent>()).Where(x => x.End > now))
            {
                var notifyAt = item.Start.AddMinutes(-settings.NotifyMinutesBefore);
                if (settings.NotifyBeforeMeeting && now >= notifyAt && now < notifyAt.AddMinutes(1) && _sent.Add(item.Id + ":start"))
                {
                    _tray.BalloonTipTitle = item.Title; _tray.BalloonTipText = item.CanJoin ? item.Countdown + " • Click MeetingBar to join" : item.Countdown; _tray.ShowBalloonTip(7000);
                    if (settings.FullscreenReminder) new ReminderWindow(item, () => _opener.Open(item)).Show();
                }
                if (settings.AutoJoin && item.CanJoin && now >= item.Start.AddMinutes(-settings.AutoJoinMinutesBefore) && now < item.Start.AddMinutes(-settings.AutoJoinMinutesBefore).AddMinutes(1) && _sent.Add(item.Id + ":auto")) _opener.Open(item);
                if (settings.NotifyAtEnd && now >= item.End && now < item.End.AddMinutes(1) && _sent.Add(item.Id + ":end"))
                { _tray.BalloonTipTitle = item.Title; _tray.BalloonTipText = "Meeting ended"; _tray.ShowBalloonTip(5000); }
            }
            _sent.RemoveWhere(id => !(_events?.Invoke() ?? new List<CalendarEvent>()).Any(x => id.StartsWith(x.Id + ":")));
        }
        public void Dispose() => _timer.Stop();
    }
}
