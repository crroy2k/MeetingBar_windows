using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace MeetingBar.Windows.Services
{
    internal sealed class DiagnosticState
    {
        public string SessionId { get; set; }
        public int ProcessId { get; set; }
        public string Version { get; set; }
        public string StartedUtc { get; set; }
        public string HeartbeatUtc { get; set; }
        public string UiHeartbeatUtc { get; set; }
        public string StoppedUtc { get; set; }
        public string State { get; set; }
        public string ExitReason { get; set; }
        public string ExitRequestedUtc { get; set; }
        public string LastIntent { get; set; }
        public string LastIntentUtc { get; set; }
        public int? ExitCode { get; set; }
    }

    // Local metadata only: exception messages, Data, calendar data and command arguments are excluded.
    internal sealed class DiagnosticJournal : IDisposable
    {
        private readonly object _gate = new object();
        private readonly string _folder, _version;
        private readonly int _processId;
        private readonly long _maximumLogBytes;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private DiagnosticState _state;
        private Timer _heartbeat;
        private long _uiTicks;
        private bool _finished, _fatal;
        public string LogPath => Path.Combine(_folder, "meetingbar.log");
        internal string StatePath => Path.Combine(_folder, "diagnostic-session.json");

        public DiagnosticJournal(string folder, string version, long maximumLogBytes = 1024 * 1024)
        {
            _folder = folder; _version = version; _maximumLogBytes = maximumLogBytes;
            using (var process = Process.GetCurrentProcess()) _processId = process.Id;
        }
        public void Begin()
        {
            Safe(() => {
                if (_state != null) return;
                if (File.Exists(StatePath))
                {
                    try
                    {
                        if (new FileInfo(StatePath).Length > 32768) throw new InvalidDataException();
                        var previous = _json.Deserialize<DiagnosticState>(File.ReadAllText(StatePath));
                        WriteCore(previous?.State == "clean-exit" ? "INFO" : "WARN",
                            "Previous session " + (previous?.State == "clean-exit" ? "closed cleanly" : "ended without confirmed clean exit")
                            + "; session=" + Field(previous?.SessionId) + "; version=" + Field(previous?.Version)
                            + "; pid=" + previous?.ProcessId + "; state=" + Field(previous?.State)
                            + "; reason=" + Field(previous?.ExitReason) + "; exitRequestedUtc=" + Field(previous?.ExitRequestedUtc)
                            + "; lastIntent=" + Field(previous?.LastIntent) + "; lastIntentUtc=" + Field(previous?.LastIntentUtc)
                            + "; lastHeartbeatUtc=" + Field(previous?.HeartbeatUtc)
                            + "; lastUiHeartbeatUtc=" + Field(previous?.UiHeartbeatUtc));
                        File.Copy(StatePath, StatePath + ".previous", true);
                    }
                    catch { WriteCore("WARN", "Previous diagnostic session unreadable; previous outcome unknown"); }
                }
                var now = DateTime.UtcNow;
                _uiTicks = now.Ticks;
                _state = new DiagnosticState { SessionId = Guid.NewGuid().ToString("N"), Version = _version, ProcessId = _processId,
                    StartedUtc = Utc(now), HeartbeatUtc = Utc(now), UiHeartbeatUtc = Utc(now), State = "running", ExitReason = "unspecified" };
                WriteCore("INFO", "Session started; version=" + Field(_version) + "; processBits=" + (IntPtr.Size * 8)
                    + "; runtime=" + Environment.Version + "; os=" + Environment.OSVersion.Version);
                SaveState();
                _heartbeat = new Timer(_ => Heartbeat(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            });
        }
        public void UiAlive() => Interlocked.Exchange(ref _uiTicks, DateTime.UtcNow.Ticks);
        internal void Heartbeat()
        {
            Safe(() => {
                if (_state == null || _finished || _fatal) return;
                _state.HeartbeatUtc = Utc(DateTime.UtcNow);
                _state.UiHeartbeatUtc = Utc(new DateTime(Interlocked.Read(ref _uiTicks), DateTimeKind.Utc));
                SaveState();
            });
        }
        public void RequestExit(string reason)
        {
            Safe(() => {
                if (_state == null || _finished || _fatal) return;
                _state.ExitReason = Field(reason); _state.State = "exiting"; _state.ExitRequestedUtc = Utc(DateTime.UtcNow);
                WriteCore("INFO", "Exit requested; reason=" + _state.ExitReason); SaveState();
            });
        }
        public void CancelExit()
        {
            Safe(() => {
                if (_state == null || _finished || _fatal) return;
                _state.ExitReason = "unspecified"; _state.State = "running"; _state.ExitRequestedUtc = null;
                WriteCore("INFO", "Windows session ending was canceled; application continues"); SaveState();
            });
        }
        public void NoteUpdate()
        {
            Safe(() => {
                if (_state == null || _finished || _fatal) return;
                _state.LastIntent = "update-installer"; _state.LastIntentUtc = Utc(DateTime.UtcNow);
                WriteCore("INFO", "Update installer requested; Windows may close this process for installation"); SaveState();
            });
        }
        public void Fatal(string area, Exception error)
        {
            Safe(() => {
                WriteCore("ERROR", "Fatal " + Field(area) + "; " + ExceptionDetails(error));
                if (_state == null || _finished) return;
                _fatal = true; _state.State = "fatal"; _state.ExitReason = "unhandled-exception";
                SaveState(); _heartbeat?.Dispose();
            });
        }
        public void Finish(int exitCode)
        {
            Safe(() => {
                if (_state == null || _finished) return;
                _finished = true; _heartbeat?.Dispose();
                _state.StoppedUtc = Utc(DateTime.UtcNow); _state.ExitCode = exitCode;
                if (!_fatal) _state.State = "clean-exit";
                WriteCore(_fatal ? "ERROR" : "INFO", "Session ended; state=" + _state.State + "; reason=" + _state.ExitReason + "; exitCode=" + exitCode);
                SaveState();
            });
        }
        public void ObserveProcessExit(int exitCode)
        {
            Safe(() => {
                if (_state == null || _finished) return;
                _heartbeat?.Dispose();
                _state.ExitCode = exitCode; _state.StoppedUtc = Utc(DateTime.UtcNow);
                if (!_fatal) _state.State = "process-exit-without-cleanup";
                WriteCore("WARN", "ProcessExit observed without completed WPF cleanup; exitCode=" + exitCode); SaveState();
            });
        }
        public void Info(string message) => Safe(() => WriteCore("INFO", message));
        public void Error(string area, Exception error) => Safe(() => WriteCore("ERROR", Field(area) + "; " + ExceptionDetails(error)));
        internal static string ExceptionDetails(Exception error, int depth = 0)
        {
            if (error == null) return "exception=unknown";
            if (depth >= 4) return "innerExceptions=truncated";
            var frames = new StackTrace(error, false).GetFrames() ?? new StackFrame[0];
            var detail = "exception=" + error.GetType().FullName + "; hresult=0x" + error.HResult.ToString("X8", CultureInfo.InvariantCulture)
                + "; methods=" + string.Join(" > ", frames.Take(16).Select(frame => {
                    var method = frame.GetMethod(); return method?.DeclaringType?.FullName + "." + method?.Name;
                }));
            var aggregate = error as AggregateException;
            if (aggregate != null) detail += "; inner=[" + string.Join(" | ", aggregate.InnerExceptions.Take(6).Select(x => ExceptionDetails(x, depth + 1))) + "]";
            else if (error.InnerException != null) detail += "; inner=[" + ExceptionDetails(error.InnerException, depth + 1) + "]";
            return detail;
        }
        private void WriteCore(string level, string message)
        {
            Directory.CreateDirectory(_folder);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length >= _maximumLogBytes)
                for (var i = 3; i >= 1; i--)
                {
                    var from = i == 1 ? LogPath : LogPath + "." + (i - 1); var to = LogPath + "." + i;
                    if (!File.Exists(from)) continue;
                    if (File.Exists(to)) File.Delete(to);
                    File.Move(from, to);
                }
            var text = (message ?? "").Replace("\r", " ").Replace("\n", " ");
            if (text.Length > 12000) text = text.Substring(0, 12000) + " [truncated]";
            File.AppendAllText(LogPath, DateTimeOffset.Now.ToString("o") + " " + level + " pid=" + _processId
                + " session=" + (_state?.SessionId ?? "none") + " " + text + Environment.NewLine, new UTF8Encoding(false));
        }
        private void SaveState()
        {
            Directory.CreateDirectory(_folder); var temporary = StatePath + ".tmp";
            File.WriteAllText(temporary, _json.Serialize(_state), new UTF8Encoding(false));
            if (File.Exists(StatePath)) File.Replace(temporary, StatePath, null); else File.Move(temporary, StatePath);
        }
        private void Safe(Action action) { try { lock (_gate) action(); } catch { /* Logging must never take down the app. */ } }
        private static string Utc(DateTime value) => value.ToString("o", CultureInfo.InvariantCulture);
        private static string Field(string value) => new string((value ?? "unknown").Take(120).Where(c => char.IsLetterOrDigit(c) || "-_.:+TZ".Contains(c)).ToArray());
        public void Dispose() { lock (_gate) _heartbeat?.Dispose(); }
    }
}
