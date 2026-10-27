using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using MeetingBar.Windows.Models;

namespace MeetingBar.Windows.Services
{
    public sealed class SettingsService
    {
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
        public string SettingsPath { get; }
        public AppSettings Current { get; private set; }

        public SettingsService()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingBar");
            Directory.CreateDirectory(folder);
            SettingsPath = Path.Combine(folder, "settings.json");
            Current = Load();
            if (Current.CompactAgendaSettingsVersion < 2)
            {
                if (Current.TimeMode != TimeMode.Hidden) Current.TimeMode = TimeMode.DateAndTime;
                Current.Use24HourClock = false;
                Current.CompactAgendaSettingsVersion = 2;
                Save();
            }
            if (Current.EnsureCalendarSources()) Save();
            if (Current.WidgetSettingsVersion < 1)
            {
                if (Current.WidgetWidth <= 300) Current.WidgetWidth = 500;
                if (Current.TitleMaxLength <= 36) Current.TitleMaxLength = 64;
                Current.WidgetSettingsVersion = 1;
                Save();
            }
            if (Current.EventWindowSettingsVersion < 1)
            {
                Current.PastMeetingHours = Current.PastMeetingHours <= 0 ? 3 : Current.PastMeetingHours;
                Current.FutureMeetingHours = Current.FutureMeetingHours <= 0 ? 24 : Current.FutureMeetingHours;
                Current.IncludeNextDay = Current.IncludeTomorrow;
                Current.EventWindowSettingsVersion = 1;
                Save();
            }
            if (Current.NotificationScheduleVersion < 1)
            {
                Current.NotificationMinutesBefore = new System.Collections.Generic.List<int> { Math.Max(0, Math.Min(120, Current.NotifyMinutesBefore)) };
                Current.NotificationScheduleVersion = 1;
                Save();
            }
        }

        private AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new AppSettings();
                return _json.Deserialize<AppSettings>(File.ReadAllText(SettingsPath, Encoding.UTF8)) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }

        public void Save()
        {
            var json = _json.Serialize(Current);
            var temporary = SettingsPath + ".tmp";
            File.WriteAllText(temporary, json, Encoding.UTF8);
            if (File.Exists(SettingsPath)) File.Replace(temporary, SettingsPath, null);
            else File.Move(temporary, SettingsPath);
        }

        public AppSettings CopyCurrent() => _json.Deserialize<AppSettings>(_json.Serialize(Current));
        public void Reload() { Current = Load(); if (Current.EnsureCalendarSources()) Save(); }
        public void Replace(AppSettings settings) { Current = settings ?? new AppSettings(); Current.EnsureCalendarSources(); Save(); }
    }
}
