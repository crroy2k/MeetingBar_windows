using System.Globalization;
using System.Threading;

namespace MeetingBar.Localization
{
    public static class SpanishText
    {
        public static CultureInfo SystemCulture { get; } = CultureInfo.CurrentCulture;
        public static CultureInfo Culture { get; } = CreateCulture();

        private static CultureInfo CreateCulture()
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("es-CR").Clone();
            // Keep the same visible designators on .NET Framework and modern .NET/ICU.
            culture.DateTimeFormat.AMDesignator = "a. m.";
            culture.DateTimeFormat.PMDesignator = "p. m.";
            return CultureInfo.ReadOnly(culture);
        }

        public static void Initialize()
        {
            CultureInfo.DefaultThreadCurrentCulture = Culture;
            CultureInfo.DefaultThreadCurrentUICulture = Culture;
            Thread.CurrentThread.CurrentCulture = Culture;
            Thread.CurrentThread.CurrentUICulture = Culture;
        }

        public static string SourceName(string value) => value == "Classic Outlook" ? "Outlook clásico"
            : value == "ICS calendar" ? "Calendario ICS" : value;
        public static string EndOfDayMessage(string value) => value == "Done for today" ? "Jornada finalizada" : value;
    }
}
