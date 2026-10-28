using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MeetingBar.Windows.Services;

namespace MeetingBar.Tests
{
    internal static class UpdateTests
    {
        private const string Tag = "v99.1.0.0";
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("test installer bytes");
        private static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private static void Assert(bool value) { if (!value) throw new Exception("Update security assertion failed"); }
        private static void Reject(Action action) { try { action(); } catch (InvalidDataException) { return; } throw new Exception("Unsafe update accepted"); }
        private static string Manifest(string tag = Tag, string digest = null, string url = null, long size = -1)
        {
            return new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                ["schema_version"] = 1, ["tag_name"] = tag, ["body"] = "<script>untrusted notes</script>",
                ["browser_download_url"] = url ?? UpdateRelease.RepositoryUrl + "/releases/download/" + tag + "/MeetingBar-Windows-v" + tag.TrimStart('v') + "-Setup.exe",
                ["digest"] = digest ?? "sha256:" + Hash(Payload), ["size"] = size == -1 ? Payload.Length : size });
        }

        public static void VersionPolicy()
        {
            Assert(UpdateRelease.ParseVersion("v0.1.0.10") > UpdateRelease.ParseVersion("v0.1.0.9"));
            Assert(UpdateRelease.ParseVersion("1.2.3") == UpdateRelease.ParseVersion("v1.2.3.0"));
            Assert(UpdateRelease.ParseVersion("v0.1.0.6") < UpdateService.InstalledVersion);
            foreach (var tag in new[] { "latest", "LASTEST", "v1.2.3-beta", "v1.2.3+build", "../1.2.3", "1.2", "v1.2.3.exe" }) Reject(() => UpdateRelease.ParseVersion(tag));
        }

        public static void MetadataPolicy()
        {
            var release = UpdateRelease.Parse(Manifest(), true);
            Assert(release.Tag == Tag && release.Size == Payload.Length && release.Notes.StartsWith("<script>"));
            Reject(() => UpdateRelease.Parse(Manifest(digest: ""), true));
            Reject(() => UpdateRelease.Parse(Manifest(digest: "sha256:bad"), true));
            Reject(() => UpdateRelease.Parse(Manifest(size: 0), true));
            Reject(() => UpdateRelease.Parse(Manifest(size: UpdateRelease.MaximumInstallerBytes + 1), true));
            foreach (var url in new[] { "http://github.com/crroy2k/MeetingBar_windows/installer.exe", "https://github.com.evil.test/installer.exe", "https://github.com/other/repo/releases/download/" + Tag + "/installer.exe" })
                Reject(() => UpdateRelease.Parse(Manifest(url: url), true));
            var json = Manifest().Replace("\"schema_version\":1", "\"schema_version\":2");
            Reject(() => UpdateRelease.Parse(json, true));
            Reject(() => UpdateRelease.Parse("{\"tag_name\":\"v9.0.0\",\"prerelease\":true}", false));
            Reject(() => UpdateRelease.Parse("{\"tag_name\":\"v9.0.0\",\"draft\":true}", false));
        }

        public static void RedirectPolicy()
        {
            Assert(UpdateService.IsTrustedRedirect(new Uri("https://release-assets.githubusercontent.com/asset")));
            Assert(UpdateService.IsTrustedRedirect(new Uri(UpdateRelease.RepositoryUrl + "/releases/download/v1/file")));
            foreach (var url in new[] { "http://release-assets.githubusercontent.com/asset", "https://release-assets.githubusercontent.com.evil.test/asset", "https://user@release-assets.githubusercontent.com/asset", "https://release-assets.githubusercontent.com:444/asset", "https://github.com/other/repo/releases/file", "file:///C:/malware.exe" })
                Assert(!UpdateService.IsTrustedRedirect(new Uri(url)));
        }

        public static async Task OnDemandAndBootstrap()
        {
            var handler = new Handler(uri => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Manifest()) });
            using (var service = new UpdateService(handler, (p, v) => { }))
            {
                Assert(handler.Requests == 0); // Creating the service never checks GitHub.
                Assert((await service.CheckAsync(CancellationToken.None)).Tag == Tag && handler.Requests == 1);
            }
            var api = new JavaScriptSerializer().Serialize(new Dictionary<string, object> {
                ["tag_name"] = Tag, ["draft"] = false, ["prerelease"] = false,
                ["assets"] = new[] { new Dictionary<string, object> { ["name"] = "MeetingBar-Windows-" + Tag + "-Setup.exe",
                    ["browser_download_url"] = UpdateRelease.RepositoryUrl + "/releases/download/" + Tag + "/MeetingBar-Windows-" + Tag + "-Setup.exe",
                    ["digest"] = "sha256:" + Hash(Payload), ["size"] = Payload.Length } } });
            handler = new Handler(uri => uri.AbsoluteUri == UpdateService.ManifestUrl ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(api) });
            using (var service = new UpdateService(handler, (p, v) => { })) Assert((await service.CheckAsync(CancellationToken.None)).Tag == Tag && handler.Requests == 2);
            handler = new Handler(uri => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Manifest(digest: "bad")) });
            using (var service = new UpdateService(handler, (p, v) => { }))
            {
                try { await service.CheckAsync(CancellationToken.None); throw new Exception("Invalid metadata accepted"); }
                catch (InvalidDataException) { Assert(handler.Requests == 1); } // Never bypass invalid metadata using another source.
            }
        }

        public static async Task DownloadIntegrity()
        {
            var validated = false;
            using (var service = new UpdateService(new Handler(uri => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Payload) }), (p, v) => validated = true))
            {
                var file = await service.DownloadAsync(UpdateRelease.Parse(Manifest(), true), null, CancellationToken.None);
                try { Assert(validated && File.ReadAllText(file) == Encoding.UTF8.GetString(Payload)); }
                finally { File.Delete(file); Directory.Delete(Path.GetDirectoryName(file)); }
            }
            foreach (var manifest in new[] { Manifest(digest: "sha256:" + new string('0', 64)), Manifest(size: Payload.Length + 1) })
            {
                using (var service = new UpdateService(new Handler(uri => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Payload) }), (p, v) => { throw new Exception("Validation must follow integrity checks"); }))
                {
                    try { await service.DownloadAsync(UpdateRelease.Parse(manifest, true), null, CancellationToken.None); throw new Exception("Damaged download accepted"); }
                    catch (InvalidDataException) { }
                }
            }
            using (var service = new UpdateService(new Handler(uri => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Payload) }), (p, v) => { }))
            {
                try { await service.DownloadAsync(UpdateRelease.Parse(Manifest(tag: "v0.1.0.6"), true), null, CancellationToken.None); throw new Exception("Downgrade accepted"); }
                catch (InvalidOperationException) { }
            }
        }

        public static async Task RejectRedirectAndOversize()
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Redirect);
            redirect.Headers.Location = new Uri("https://evil.test/installer.exe");
            using (var service = new UpdateService(new Handler(uri => redirect), (p, v) => { }))
            {
                try { await service.CheckAsync(CancellationToken.None); throw new Exception("Untrusted redirect followed"); }
                catch (InvalidDataException) { }
            }
            using (var service = new UpdateService(new Handler(uri => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', 256 * 1024 + 1)) }), (p, v) => { }))
            {
                try { await service.CheckAsync(CancellationToken.None); throw new Exception("Oversize metadata accepted"); }
                catch (InvalidDataException) { }
            }
        }

        private sealed class Handler : HttpMessageHandler
        {
            private readonly Func<Uri, HttpResponseMessage> _respond;
            public int Requests { get; private set; }
            public Handler(Func<Uri, HttpResponseMessage> respond) { _respond = respond; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            { cancellationToken.ThrowIfCancellationRequested(); Requests++; return Task.FromResult(_respond(request.RequestUri)); }
        }
    }
}
