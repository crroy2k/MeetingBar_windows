using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MeetingBar.Display
{
    public sealed class MonitorChoice
    {
        public string DeviceName { get; set; } = "";
        public bool Primary { get; set; }
        public int Left, Top, Right, Bottom, WorkLeft, WorkTop, WorkRight, WorkBottom;
        public override string ToString() => DeviceName + (Primary ? " · Principal" : "") + " · " + (Right - Left) + " × " + (Bottom - Top);
    }
    public static class MonitorCatalog
    {
        [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct Info
        {
            public int Size; public Rect Bounds, Work; public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Device;
        }
        private delegate bool Callback(IntPtr monitor, IntPtr dc, IntPtr rect, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, Callback callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")] private static extern bool GetMonitorInfo(IntPtr monitor, ref Info info);
        public static List<MonitorChoice> GetAll()
        {
            var result = new List<MonitorChoice>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, dc, rect, data) =>
            {
                var info = new Info { Size = Marshal.SizeOf(typeof(Info)) };
                if (GetMonitorInfo(monitor, ref info)) result.Add(new MonitorChoice { DeviceName = info.Device, Primary = (info.Flags & 1) != 0,
                    Left = info.Bounds.Left, Top = info.Bounds.Top, Right = info.Bounds.Right, Bottom = info.Bounds.Bottom,
                    WorkLeft = info.Work.Left, WorkTop = info.Work.Top, WorkRight = info.Work.Right, WorkBottom = info.Work.Bottom });
                return true;
            }, IntPtr.Zero);
            return result;
        }
        #pragma warning disable CS8603 // No connected displays is handled by callers.
        public static MonitorChoice Resolve(IEnumerable<MonitorChoice> monitors, string device) =>
            monitors.FirstOrDefault(m => string.Equals(m.DeviceName, device, StringComparison.OrdinalIgnoreCase)) ?? monitors.FirstOrDefault(m => m.Primary) ?? monitors.FirstOrDefault();
    }
}
