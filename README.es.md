# MeetingBar para Windows

MeetingBar muestra la reunión actual o siguiente en la bandeja del sistema y junto a la barra de tareas. Permite consultar la agenda, recibir recordatorios y abrir los enlaces de reunión.

## Versión 0.1.0.7

En Configuración → General, pulsa **Actualizar…** para consultar la versión marcada como **Latest** en GitHub. No se realizan comprobaciones automáticas al iniciar ni en segundo plano. La app muestra la versión instalada en pequeño, informa si hay una posterior y permite descargar e instalarla con confirmación.

La descarga verifica el repositorio de origen, el tamaño, el SHA-256 y la versión del instalador antes de ejecutarlo. Requiere acceso a GitHub por HTTPS; si no hay conexión, puedes seguir usando Outlook y consultar de nuevo más tarde. La integridad se contrasta con los metadatos publicados en GitHub y no constituye una firma digital independiente.

La actualización conserva tus ajustes y vuelve a abrir MeetingBar. Guarda primero los cambios pendientes de Configuración. Para incorporar esta función desde la 0.1.0.6, instala una vez el nuevo instalador.

## Mejoras anteriores

- «Abrir calendario» abre Outlook clásico directamente, sin depender de la asociación de Windows con el nuevo Outlook.
- La agenda, los detalles, los recordatorios, la configuración y el instalador están en español.
- Las fechas y horas se muestran en español. Se conserva la elección entre 12 y 24 horas.
- La actualización conserva las cuentas, los calendarios seleccionados, los filtros y los avisos. Los nombres y mensajes personalizados se conservan; solo se traducen las etiquetas predeterminadas conocidas.

## Instalación y actualización

Ejecuta el instalador y sigue las instrucciones. Se instala para el usuario actual y no requiere permisos de administrador. Al actualizar se guarda una copia de la configuración anterior en `%APPDATA%\MeetingBar\settings.json.before-install.bak`.

Para la versión portátil, extrae todo el paquete y abre `MeetingBar.exe`. Mantén las carpetas `Settings` y `WinWWidget` junto al ejecutable.

## Uso

- Pulsa el widget o el icono de la bandeja para abrir la agenda.
- «Unirse a la reunión» abre el enlace de la cita actual o siguiente.
- «Crear reunión» abre el enlace configurado para crear una reunión.
- «Abrir calendario» abre el calendario de la fuente correspondiente. Con Outlook, abre Outlook clásico.
- El botón de flecha circular actualiza los calendarios. El botón de ajustes abre Configuración.
- En Configuración, pulsa «Guardar» para aplicar los cambios sin cerrar la ventana.
- Ctrl+Alt+J permite unirse a la reunión actual o siguiente si el atajo está habilitado.

## Calendarios compatibles

**Outlook clásico:** lee los calendarios locales mediante la interfaz COM de Outlook. No modifica las citas. El nuevo Outlook no ofrece esa interfaz y no sustituye a Outlook clásico en esta integración.

**ICS:** acepta archivos locales y suscripciones HTTP(S), webcal y webcals. Su lector admite recurrencias básicas; las reglas complejas y las excepciones de calendario requieren validación adicional.

**Google Calendar:** requiere habilitar la API de Calendar y crear un cliente OAuth para aplicaciones de escritorio. Sigue los pasos de Configuración → Calendarios. El acceso al calendario es de solo lectura y los tokens se cifran para el usuario de Windows.

Si una fuente falla al actualizarse, se conservan sus últimos eventos válidos mientras se actualizan las demás.

## Configuración y diagnóstico

La configuración está en `%APPDATA%\MeetingBar\settings.json`. El registro de diagnóstico está en `%APPDATA%\MeetingBar\meetingbar.log`.

La sección «Pruebas» permite comprobar la interfaz con eventos locales que no se guardan en el calendario. «Quitar eventos de prueba» elimina únicamente esos eventos simulados.

## Compatibilidad

Requiere Windows 10 o 11 de 64 bits y .NET Framework 4.7.2 o posterior. Los componentes modernos incluyen sus propios entornos de ejecución.

El widget de la barra de tareas es una ventana superpuesta. La tarjeta opcional de Win+W está en fase beta y requiere el modo de desarrollador y fijarla manualmente desde el panel de widgets de Windows 11.

## Compilación y pruebas

Con el SDK de .NET 8 o posterior en Windows:

```powershell
.\build.ps1 -Configuration Release -Test -Package
```

Para crear el instalador, compila `Installer/ConfigTool/ConfigTool.csproj` en Release y usa Inno Setup 6 con `Installer/MeetingBar.iss`.

Código fuente: https://github.com/crroy2k/MeetingBar_windows

Licencia Apache 2.0. Consulta `LICENSE` y `THIRD-PARTY-NOTICES.md` para las atribuciones de terceros.
