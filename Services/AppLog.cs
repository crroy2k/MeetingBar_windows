using System;
using System.Reflection;

namespace MeetingBar.Windows.Services
{
    internal static class AppLog
    {
        private static readonly DiagnosticJournal Journal = new DiagnosticJournal(
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MeetingBar"),
            typeof(AppLog).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown");
        public static string Path => Journal.LogPath;
        public static void BeginSession() => Journal.Begin();
        public static void UiAlive() => Journal.UiAlive();
        public static void RequestExit(string reason) => Journal.RequestExit(reason);
        public static void CancelExit() => Journal.CancelExit();
        public static void UpdateRequested() => Journal.NoteUpdate();
        public static void Finish(int exitCode) => Journal.Finish(exitCode);
        public static void ProcessExit(int exitCode) => Journal.ObserveProcessExit(exitCode);
        public static void Fatal(string area, Exception error) => Journal.Fatal(area, error);
        public static void Info(string message) => Journal.Info(message);
        public static void Error(string area, Exception error) => Journal.Error(area, error);
    }
}
