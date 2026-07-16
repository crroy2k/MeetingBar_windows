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
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);
        [DllImport("dwmapi.dll")] public static extern int DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);
        [DllImport("dwmapi.dll")] public static extern int DwmFlush();
        [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
        [DllImport("dwmapi.dll")] public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [StructLayout(LayoutKind.Sequential)] public struct MARGINS { public int Left, Right, Top, Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct ACCENTPOLICY { public int State, Flags, GradientColor, AnimationId; }
        [StructLayout(LayoutKind.Sequential)] private struct WINDOWCOMPOSITIONATTRIBDATA { public int Attribute; public IntPtr Data; public int SizeOfData; }
        public const uint ModAlt = 0x0001, ModControl = 0x0002;
        public const int WmHotKey = 0x0312;
        public static readonly IntPtr HwndTopmost = new IntPtr(-1);
        public const uint SwpNoSize = 0x0001, SwpNoMove = 0x0002, SwpNoZOrder = 0x0004, SwpNoActivate = 0x0010, SwpShowWindow = 0x0040, SwpNoSendChanging = 0x0400;
        public static bool EnableTransientBackdrop(IntPtr hwnd, bool dark, bool rounded = true)
        {
            if (Environment.OSVersion.Version.Build < 22621) return false;
            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            var corner = rounded ? 2 : 3; var darkMode = dark ? 1 : 0; var hostBackdrop = 1;
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            DwmSetWindowAttribute(hwnd, 17, ref hostBackdrop, sizeof(int));
            DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));
            DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
            if (EnableAcrylicComposition(hwnd, dark ? (byte)168 : (byte)150, dark ? (byte)32 : (byte)245))
            {
                var none = 1;
                DwmSetWindowAttribute(hwnd, 38, ref none, sizeof(int));
                return true;
            }
            var backdrop = 3;
            return DwmSetWindowAttribute(hwnd, 38, ref backdrop, sizeof(int)) == 0;
        }

        public static bool EnableMainWindowBackdrop(IntPtr hwnd, bool dark)
        {
            if (Environment.OSVersion.Version.Build < 22621) return false;
            var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            var backdrop = 2; var corner = 2; var darkMode = dark ? 1 : 0;
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            DwmSetWindowAttribute(hwnd, 33, ref corner, sizeof(int));
            DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
            return DwmSetWindowAttribute(hwnd, 38, ref backdrop, sizeof(int)) == 0;
        }

        private static bool EnableAcrylicComposition(IntPtr hwnd, byte alpha, byte tint)
        {
            var policy = new ACCENTPOLICY
            {
                State = 4,
                GradientColor = unchecked((int)((uint)alpha << 24 | (uint)tint << 16 | (uint)tint << 8 | tint))
            };
            var size = Marshal.SizeOf(typeof(ACCENTPOLICY));
            var pointer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(policy, pointer, false);
                var data = new WINDOWCOMPOSITIONATTRIBDATA { Attribute = 19, Data = pointer, SizeOfData = size };
                return SetWindowCompositionAttribute(hwnd, ref data) != 0;
            }
            finally { Marshal.FreeHGlobal(pointer); }
        }
    }
}
