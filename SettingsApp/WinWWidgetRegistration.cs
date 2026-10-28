using Windows.Management.Deployment;

namespace MeetingBar.SettingsApp;

internal static class WinWWidgetRegistration
{
    private const string PackageName = "MeetingBar.WinWWidget";

    public static async Task<string> ApplyAsync(bool enabled)
    {
        var manager = new PackageManager();
        if (!enabled)
        {
            var packages = manager.FindPackagesForUser(string.Empty, PackageName).ToList();
            foreach (var package in packages)
                await manager.RemovePackageAsync(package.Id.FullName, RemovalOptions.PreserveApplicationData).AsTask();
            return packages.Count == 0 ? "Configuración guardada y aplicada" : "Se quitó el proveedor del widget de Win+W";
        }

        var manifest = FindManifest();
        if (manifest == null) throw new FileNotFoundException("No se encuentra el proveedor de Win+W. Reinstala MeetingBar o extrae de nuevo el paquete completo.");
        var manifestFolder = Path.GetDirectoryName(manifest)!;
        var existing = manager.FindPackagesForUser(string.Empty, PackageName).FirstOrDefault();
        if (existing != null)
        {
            if (string.Equals(Path.GetFullPath(existing.InstalledLocation.Path), manifestFolder, StringComparison.OrdinalIgnoreCase)) return "Configuración guardada y aplicada";
            await manager.RemovePackageAsync(existing.Id.FullName, RemovalOptions.PreserveApplicationData).AsTask();
        }
        try { await manager.RegisterPackageAsync(new Uri(manifest), null, DeploymentOptions.DevelopmentMode).AsTask(); }
        catch (Exception error) when (unchecked((uint)error.HResult) == 0x80073CFF)
        { throw new InvalidOperationException("Activa el modo de desarrollador en la configuración de Windows y vuelve a guardar.", error); }
        return "Widget instalado. Pulsa Win+W, elige «Agregar widgets» y fija MeetingBar.";
    }

    private static string? FindManifest()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "WinWWidget", "AppxManifest.xml")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WidgetProvider", "bin", "Release", "publish-folder", "AppxManifest.xml"))
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
