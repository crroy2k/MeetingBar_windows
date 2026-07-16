// COM registration pattern adapted from Microsoft's WindowsAppSDK-Samples Widgets sample (MIT).
using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;
namespace MeetingBar.WidgetProvider;
internal sealed class RegistrationManager<T> : IDisposable where T : IWidgetProvider, new()
{
    private readonly uint _cookie; private readonly ManualResetEvent _disposed = new(false); private RegistrationManager(uint cookie) => _cookie = cookie;
    public static RegistrationManager<T> RegisterProvider() { var result = NativeMethods.CoRegisterClassObject(typeof(T).GUID, new ProviderFactory<T>(), 0x4, 0x1, out var cookie); if (result != 0) Marshal.ThrowExceptionForHR(result); return new RegistrationManager<T>(cookie); }
    public ManualResetEvent GetDisposedEvent() => _disposed;
    public void Dispose() { NativeMethods.CoRevokeClassObject(_cookie); _disposed.Set(); _disposed.Dispose(); }
}
internal static class NativeMethods
{
    [DllImport("ole32.dll")] internal static extern int CoRegisterClassObject([MarshalAs(UnmanagedType.LPStruct)] Guid clsid,[MarshalAs(UnmanagedType.IUnknown)] object factory,uint context,uint flags,out uint cookie);
    [DllImport("ole32.dll")] internal static extern int CoRevokeClassObject(uint cookie);
}
[ComImport,InterfaceType(ComInterfaceType.InterfaceIsIUnknown),Guid("00000001-0000-0000-C000-000000000046")]
internal interface IClassFactory { [PreserveSig] int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr instance); [PreserveSig] int LockServer(bool locked); }
internal sealed class ProviderFactory<T> : IClassFactory where T : IWidgetProvider,new()
{
    public int CreateInstance(IntPtr outer, ref Guid iid, out IntPtr instance) { instance=IntPtr.Zero; if(outer!=IntPtr.Zero)return unchecked((int)0x80040110); if(iid!=typeof(T).GUID&&iid!=new Guid("00000000-0000-0000-C000-000000000046"))return unchecked((int)0x80004002); instance=MarshalInspectable<IWidgetProvider>.FromManaged(new T()); return 0; }
    public int LockServer(bool locked)=>0;
}
