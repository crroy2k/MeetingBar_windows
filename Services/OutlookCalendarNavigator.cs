using System;
using System.Runtime.InteropServices;

namespace MeetingBar.Windows.Services
{
    // Outlook's URI association can point to New Outlook. COM always targets Classic Outlook.
    internal static class OutlookCalendarNavigator
    {
        public static void Open(string calendarId = null)
        {
            object application = null, session = null, folder = null, explorer = null;
            try
            {
                var outlookType = Type.GetTypeFromProgID("Outlook.Application");
                if (outlookType == null)
                    throw new InvalidOperationException("Outlook clásico no está instalado en este equipo.");
                try { application = Marshal.GetActiveObject("Outlook.Application"); }
                catch (COMException) { application = Activator.CreateInstance(outlookType); }
                session = ((dynamic)application).GetNamespace("MAPI");
                folder = string.IsNullOrWhiteSpace(calendarId)
                    ? ((dynamic)session).GetDefaultFolder(9) // olFolderCalendar
                    : ((dynamic)session).GetFolderFromID(calendarId);
                explorer = ((dynamic)folder).GetExplorer();
                ((dynamic)explorer).Display();
                ((dynamic)explorer).Activate();
            }
            catch (Exception error)
            {
                throw new InvalidOperationException("No se pudo abrir el calendario de Outlook clásico. Comprueba que tu perfil de Outlook esté disponible e inténtalo de nuevo.", error);
            }
            finally
            {
                Release(explorer); Release(folder); Release(session); Release(application);
            }
        }

        private static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value);
        }
    }
}
