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
        public void Replace(AppSettings settings) { Current = settings ?? new AppSettings(); Save(); }
    }
}
