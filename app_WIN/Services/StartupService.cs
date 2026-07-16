using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace MeetingBar.Windows.Services
{
    public static class StartupService
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(KeyPath, true) ?? Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                if (enabled) key.SetValue("MeetingBar", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                else key.DeleteValue("MeetingBar", false);
            }
        }
    }
}
