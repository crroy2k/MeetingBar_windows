using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var installPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MeetingBar.exe");
            if (args.Length == 1 && args[0] == "--uninstall")
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    if (string.Equals(Convert.ToString(key?.GetValue("MeetingBar")), "\"" + installPath + "\"", StringComparison.OrdinalIgnoreCase)) key.DeleteValue("MeetingBar", false);
                return 0;
            }
            if (args.Length != 0) return 2;
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingBar");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "settings.json");
            var json = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
            var settings = File.Exists(path) ? json.Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8)) : new Dictionary<string, object>();
            if (settings == null) throw new InvalidDataException("La configuración existente no es válida; se ha conservado sin cambios.");
            if (!settings.ContainsKey("AutoJoin")) settings["AutoJoin"] = false;
            if (!settings.ContainsKey("StartWithWindows")) settings["StartWithWindows"] = true;
            if (!settings.ContainsKey("Provider")) settings["Provider"] = 0;
            var temporary = path + ".install.tmp";
            File.WriteAllText(temporary, json.Serialize(settings), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".before-install.bak"); else File.Move(temporary, path);
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (Convert.ToBoolean(settings["StartWithWindows"])) key.SetValue("MeetingBar", "\"" + installPath + "\"");
                else key.DeleteValue("MeetingBar", false);
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
