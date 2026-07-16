using System;
using System.Runtime.InteropServices;

namespace MeetingBar.Windows.Interop
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowName);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        public const uint ModAlt = 0x0001, ModControl = 0x0002;
        public const int WmHotKey = 0x0312;
    }
}
