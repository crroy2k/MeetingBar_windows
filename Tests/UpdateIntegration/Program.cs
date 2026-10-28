using System;
using System.IO;
using System.Reflection;
using System.Threading;
using MeetingBar.Windows.Services;

[assembly: AssemblyFileVersion("0.1.0.6")]
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            using (var service = new UpdateService())
            {
                var release = service.CheckAsync(CancellationToken.None).GetAwaiter().GetResult();
                Console.WriteLine("Latest: " + release.Tag + "; simulated installed version: " + UpdateService.InstalledVersion);
                if (args.Length > 0 && args[0] == "--download")
                {
                    if (release.Version <= UpdateService.InstalledVersion) throw new Exception("No newer release available for integration test");
                    var installer = service.DownloadAsync(release, null, CancellationToken.None).GetAwaiter().GetResult();
                    try
                    {
                        Console.WriteLine("PASS actual GitHub download, byte count, SHA-256 and installer product/version verified");
                        if (args.Length > 1) File.Copy(installer, Path.GetFullPath(args[1]), false);
                    }
                    finally { File.Delete(installer); Directory.Delete(Path.GetDirectoryName(installer)); }
                }
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
}
