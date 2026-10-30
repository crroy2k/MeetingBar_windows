using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using MeetingBar.Windows.Services;

namespace MeetingBar.Tests
{
    internal static class DiagnosticTests
    {
        internal static bool RunChild(string[] args)
        {
            if (args.Length < 2 || args[0] != "--diagnostics-child") return false;
            using (var journal = new DiagnosticJournal(args[1], "0.1.0.10"))
            {
                journal.Begin(); journal.UiAlive(); journal.Heartbeat();
                File.WriteAllText(Path.Combine(args[1], "ready"), "ready");
                Thread.Sleep(Timeout.Infinite);
            }
            return true;
        }
        private static void Assert(bool value) { if (!value) throw new Exception("Diagnostics assertion failed"); }
        private static DiagnosticState Read(string folder) => new JavaScriptSerializer().Deserialize<DiagnosticState>(File.ReadAllText(Path.Combine(folder, "diagnostic-session.json")));
        private static string Folder() => Path.Combine(Path.GetTempPath(), "MeetingBar-DiagnosticsTests-" + Guid.NewGuid().ToString("N"));
        private static void Cleanup(string folder)
        {
            var absolute = Path.GetFullPath(folder);
            if (absolute.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                && Path.GetFileName(absolute).StartsWith("MeetingBar-DiagnosticsTests-", StringComparison.Ordinal) && Directory.Exists(absolute))
                Directory.Delete(absolute, true);
        }
        public static void CleanExit()
        {
            var folder = Folder();
            try
            {
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10"))
                {
                    journal.Begin(); journal.Begin(); journal.UiAlive(); journal.Heartbeat();
                    journal.RequestExit("windows-shutdown"); journal.CancelExit();
                    Assert(Read(folder).State == "running" && Read(folder).ExitRequestedUtc == null);
                    journal.NoteUpdate();
                    Assert(Read(folder).LastIntent == "update-installer" && Read(folder).LastIntentUtc != null);
                    journal.RequestExit("tray-menu"); journal.Finish(0); journal.ObserveProcessExit(0);
                }
                var state = Read(folder);
                Assert(state.State == "clean-exit" && state.ExitReason == "tray-menu" && state.ExitCode == 0 && state.ProcessId > 0 && state.UiHeartbeatUtc != null);
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10")) { journal.Begin(); journal.Finish(0); }
                Assert(File.ReadAllText(Path.Combine(folder, "meetingbar.log")).Contains("Previous session closed cleanly"));
                Assert(File.Exists(Path.Combine(folder, "diagnostic-session.json.previous")));
            }
            finally { Cleanup(folder); }
        }
        public static void ForcedTermination()
        {
            var folder = Folder(); Process child = null;
            try
            {
                child = Process.Start(new ProcessStartInfo(Assembly.GetExecutingAssembly().Location,
                    "--diagnostics-child \"" + folder + "\"") { UseShellExecute = false, CreateNoWindow = true });
                var timeout = Stopwatch.StartNew();
                while (!File.Exists(Path.Combine(folder, "ready")) && !child.HasExited && timeout.ElapsedMilliseconds < 10000) Thread.Sleep(25);
                Assert(File.Exists(Path.Combine(folder, "ready")));
                Assert(Read(folder).ProcessId == child.Id);
                child.Kill(); Assert(child.WaitForExit(5000));
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10")) { journal.Begin(); journal.Finish(0); }
                var log = File.ReadAllText(Path.Combine(folder, "meetingbar.log"));
                Assert(log.Contains("ended without confirmed clean exit") && log.Contains("lastHeartbeatUtc=") && log.Contains("lastUiHeartbeatUtc="));
            }
            finally { if (child != null) { if (!child.HasExited) { child.Kill(); child.WaitForExit(5000); } child.Dispose(); } Cleanup(folder); }
        }
        public static void FatalAndIncompleteExit()
        {
            var folder = Folder();
            try
            {
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10"))
                { journal.Begin(); journal.Fatal("Unhandled", new InvalidOperationException("private")); journal.Finish(23); }
                Assert(Read(folder).State == "fatal" && Read(folder).ExitReason == "unhandled-exception");
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10"))
                { journal.Begin(); journal.RequestExit("ipc-quit"); journal.ObserveProcessExit(2); }
                Assert(Read(folder).State == "process-exit-without-cleanup" && Read(folder).ExitCode == 2);
                File.WriteAllText(Path.Combine(folder, "diagnostic-session.json"), "{broken");
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10")) { journal.Begin(); journal.Finish(0); }
                Assert(File.ReadAllText(Path.Combine(folder, "meetingbar.log")).Contains("previous outcome unknown"));
            }
            finally { Cleanup(folder); }
        }
        public static void RotationAndPrivacy()
        {
            var folder = Folder();
            try
            {
                Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "meetingbar.log.old"), "legacy archive");
                using (var journal = new DiagnosticJournal(folder, "0.1.0.10", 512))
                {
                    journal.Begin();
                    for (var i = 0; i < 80; i++) journal.Info("rotation " + i);
                    try { ThrowPrivateError(); } catch (Exception error) { error.Data["secret-token"] = "NEVER-LOG"; journal.Error("test", error); }
                    journal.Finish(0);
                }
                var log = File.ReadAllText(Path.Combine(folder, "meetingbar.log")) + File.ReadAllText(Path.Combine(folder, "meetingbar.log.1"));
                Assert(log.Contains("InvalidOperationException") && log.Contains("IOException") && log.Contains("hresult=0x") && log.Contains("ThrowPrivateError"));
                Assert(!log.Contains("SECRET-MEETING") && !log.Contains("https://") && !log.Contains("NEVER-LOG"));
                Assert(File.Exists(Path.Combine(folder, "meetingbar.log.3")) && !File.Exists(Path.Combine(folder, "meetingbar.log.4")));
                var blocked = Path.Combine(folder, "blocked"); File.WriteAllText(blocked, "not a directory");
                using (var journal = new DiagnosticJournal(blocked, "0.1.0.10")) { journal.Begin(); journal.Info("no throw"); journal.RequestExit("tray-menu"); journal.Finish(0); }
                Assert(File.ReadAllText(blocked) == "not a directory");
            }
            finally { Cleanup(folder); }
        }
        private static void ThrowPrivateError() => throw new InvalidOperationException("SECRET-MEETING https://private.example/NEVER-LOG", new IOException("NEVER-LOG"));
    }
}
