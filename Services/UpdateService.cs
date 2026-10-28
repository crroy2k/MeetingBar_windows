using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MeetingBar.Windows.Services
{
    public sealed class UpdateRelease
    {
        internal const string RepositoryUrl = "https://github.com/crroy2k/MeetingBar_windows";
        internal const long MaximumInstallerBytes = 512L * 1024 * 1024;
        public Version Version { get; private set; }
        public string Tag { get; private set; }
        public string Notes { get; private set; }
        public string DownloadUrl { get; private set; }
        public string Sha256 { get; private set; }
        public long Size { get; private set; }

        internal static Version ParseVersion(string tag)
        {
            if (!Regex.IsMatch(tag ?? "", @"^v?\d{1,5}\.\d{1,5}\.\d{1,5}(?:\.\d{1,5})?$", RegexOptions.CultureInvariant))
                throw new InvalidDataException("La versión publicada no tiene un formato compatible.");
            var parsed = Version.Parse(tag.TrimStart('v'));
            return new Version(parsed.Major, parsed.Minor, parsed.Build, Math.Max(0, parsed.Revision));
        }

        internal static UpdateRelease Parse(string json, bool manifest)
        {
            var root = new JavaScriptSerializer { MaxJsonLength = 256 * 1024 }.Deserialize<Dictionary<string, object>>(json);
            if (root == null) throw new InvalidDataException("La información de actualización está vacía.");
            string Text(Dictionary<string, object> obj, string key) => obj.ContainsKey(key) ? Convert.ToString(obj[key]) : null;
            if (!manifest && (root.ContainsKey("draft") && Convert.ToBoolean(root["draft"]) || root.ContainsKey("prerelease") && Convert.ToBoolean(root["prerelease"])))
                throw new InvalidDataException("Solo se permiten versiones estables publicadas.");
            if (manifest && (!root.ContainsKey("schema_version") || Convert.ToInt32(root["schema_version"]) != 1))
                throw new InvalidDataException("El formato de actualización no es compatible.");
            var tag = Text(root, "tag_name");
            var version = ParseVersion(tag);
            var expectedName = "MeetingBar-Windows-v" + tag.TrimStart('v') + "-Setup.exe";
            var expectedUrl = RepositoryUrl + "/releases/download/" + tag + "/" + expectedName;
            var asset = manifest ? root : (root.ContainsKey("assets") ? root["assets"] as IEnumerable : null)?
                .Cast<object>().OfType<Dictionary<string, object>>().SingleOrDefault(x => Text(x, "name") == expectedName);
            if (asset == null) throw new InvalidDataException("La versión publicada aún no incluye el instalador de Windows.");
            var url = Text(asset, "browser_download_url");
            if (!string.Equals(url, expectedUrl, StringComparison.Ordinal))
                throw new InvalidDataException("El instalador no pertenece a la versión esperada del repositorio.");
            var digest = Text(asset, "digest");
            if (!Regex.IsMatch(digest ?? "", @"^sha256:[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant))
                throw new InvalidDataException("El instalador no incluye un SHA-256 válido. No se descargará.");
            var size = asset.ContainsKey("size") ? Convert.ToInt64(asset["size"]) : 0;
            if (size < 1 || size > MaximumInstallerBytes) throw new InvalidDataException("El tamaño del instalador no es válido.");
            return new UpdateRelease { Version = version, Tag = tag, Notes = Text(root, "body") ?? "", DownloadUrl = url, Sha256 = digest.Substring(7).ToLowerInvariant(), Size = size };
        }
    }

    public sealed class UpdateService : IDisposable
    {
        internal const string ManifestUrl = UpdateRelease.RepositoryUrl + "/releases/latest/download/update.json";
        internal const string ApiUrl = "https://api.github.com/repos/crroy2k/MeetingBar_windows/releases/latest";
        private readonly HttpClient _http;
        private readonly Action<string, Version> _validateExecutable;
        public static Version InstalledVersion => UpdateRelease.ParseVersion(
            typeof(UpdateService).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.0.0");

        public UpdateService() : this(new HttpClientHandler { AllowAutoRedirect = false }, ValidateExecutable) { }
        internal UpdateService(HttpMessageHandler handler, Action<string, Version> validateExecutable)
        {
            _http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MeetingBar-Windows/" + InstalledVersion);
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            _validateExecutable = validateExecutable;
        }

        public async Task<UpdateRelease> CheckAsync(CancellationToken cancellationToken)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                using (var response = await SendAsync(new Uri(ManifestUrl), timeout.Token).ConfigureAwait(false))
                {
                    if (response.StatusCode != HttpStatusCode.NotFound)
                    {
                        response.EnsureSuccessStatusCode();
                        return UpdateRelease.Parse(await ReadMetadata(response, timeout.Token).ConfigureAwait(false), true);
                    }
                }
                // Bootstrap releases published before update.json existed.
                using (var response = await SendAsync(new Uri(ApiUrl), timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return UpdateRelease.Parse(await ReadMetadata(response, timeout.Token).ConfigureAwait(false), false);
                }
            }
        }

        public async Task<string> DownloadAsync(UpdateRelease release, IProgress<int> progress, CancellationToken cancellationToken)
        {
            if (release == null || release.Version <= InstalledVersion) throw new InvalidOperationException("No hay una versión posterior para instalar.");
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeetingBar", "Updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var partial = Path.Combine(folder, "installer.partial");
            var destination = Path.Combine(folder, "MeetingBar-" + release.Tag + "-Setup.exe");
            try
            {
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeout.CancelAfter(TimeSpan.FromMinutes(15));
                    using (var response = await SendAsync(new Uri(release.DownloadUrl), timeout.Token).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value != release.Size)
                            throw new InvalidDataException("El tamaño de la descarga no coincide con el publicado.");
                        using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                        {
                            var buffer = new byte[81920]; long total = 0; int read; var lastPercent = -1;
                            while ((read = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) != 0)
                            {
                                total += read;
                                if (total > release.Size) throw new InvalidDataException("La descarga supera el tamaño permitido.");
                                await output.WriteAsync(buffer, 0, read, timeout.Token).ConfigureAwait(false);
                                var percent = (int)(total * 100 / release.Size);
                                if (percent != lastPercent) { progress?.Report(percent); lastPercent = percent; }
                            }
                            if (total != release.Size) throw new InvalidDataException("La descarga está incompleta.");
                        }
                    }
                    timeout.Token.ThrowIfCancellationRequested();
                    VerifyHash(partial, release.Sha256);
                    _validateExecutable(partial, release.Version);
                    File.Move(partial, destination);
                    return destination;
                }
            }
            catch
            {
                try { File.Delete(partial); Directory.Delete(folder); } catch { }
                throw;
            }
        }

        internal static void VerifyHash(string path, string expected)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(actual, expected, StringComparison.Ordinal)) throw new InvalidDataException("La verificación SHA-256 falló. El instalador no se ejecutará.");
            }
        }

        private static void ValidateExecutable(string path, Version expected)
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            if (UpdateRelease.ParseVersion((info.ProductVersion ?? "").Trim()) != expected || (info.ProductName ?? "").Trim() != "MeetingBar")
                throw new InvalidDataException("El ejecutable descargado no corresponde a MeetingBar o a la versión publicada.");
        }

        public void Install(string path, UpdateRelease release)
        {
            // Revalidate after the user confirms, not just when the download finishes.
            var root = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeetingBar", "Updates")) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase) || release.Version <= InstalledVersion)
                throw new InvalidOperationException("El instalador no pertenece a la carpeta de actualizaciones.");
            VerifyHash(path, release.Sha256); ValidateExecutable(path, release.Version);
            var arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /OTA=1 /DIR=\"" + AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar) + "\"";
            var process = Process.Start(new ProcessStartInfo(path, arguments) { UseShellExecute = true });
            if (process == null) throw new InvalidOperationException("No se pudo iniciar el instalador.");
            process.Dispose();
        }

        internal static bool IsTrustedRedirect(Uri uri) => uri != null && uri.Scheme == Uri.UriSchemeHttps
            && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) && (uri.Host == "release-assets.githubusercontent.com"
            || uri.Host == "objects.githubusercontent.com" || uri.Host == "github-releases.githubusercontent.com"
            || uri.Host == "github.com" && uri.AbsolutePath.StartsWith("/crroy2k/MeetingBar_windows/releases/", StringComparison.Ordinal));

        private async Task<HttpResponseMessage> SendAsync(Uri uri, CancellationToken token)
        {
            for (var redirects = 0; redirects < 6; redirects++)
            {
                var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400) return response;
                var location = response.Headers.Location;
                response.Dispose();
                if (location == null) throw new InvalidDataException("La descarga devolvió una redirección vacía.");
                uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if (!IsTrustedRedirect(uri)) throw new InvalidDataException("La actualización intentó redirigir a un destino no permitido.");
            }
            throw new InvalidDataException("La actualización devolvió demasiadas redirecciones.");
        }

        private static async Task<string> ReadMetadata(HttpResponseMessage response, CancellationToken token)
        {
            using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[8192]; int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
                {
                    if (output.Length + read > 256 * 1024) throw new InvalidDataException("La información de actualización supera el tamaño permitido.");
                    output.Write(buffer, 0, read);
                }
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        public void Dispose() => _http.Dispose();
    }
}
