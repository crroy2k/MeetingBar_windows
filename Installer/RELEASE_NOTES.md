# Versión 0.1.0.9 — Eventos programados en la agenda

- La vista inicial mantiene la tarjeta de la próxima cita y agrega debajo «Programados», con una separación sutil.
- Muestra los eventos posteriores en orden cronológico, sin repetir la cita principal. Cada fila incluye título, fecha y hora, calendario y contador del tiempo restante.
- La lista se actualiza con la sincronización habitual del calendario. Los contadores se refrescan con el reloj de la aplicación, sin consultas adicionales a Outlook.
- Los eventos finalizados desaparecen de la lista y, al avanzar la próxima cita, se actualizan los eventos que aparecen debajo.
- Conserva la navegación por días desde la línea de tiempo, los filtros de calendario, los servicios configurables y las actualizaciones exclusivamente manuales.

## Cómo actualizar

En Configuración → General → Buscar actualizaciones, descarga y confirma la nueva versión. Se conservan los calendarios y tus ajustes.

## Historial

### Versión 0.1.0.8 — Servicios configurables y ajustes integrados

- «Buscar actualizaciones» comprueba GitHub y muestra el resultado en Configuración → General. La descarga, el progreso y la confirmación de instalación permanecen en esa misma ventana.
- Las comprobaciones siguen siendo exclusivamente manuales. No se busca ni descarga al iniciar o en segundo plano.
- «Crear reunión» muestra un menú con los servicios que configures en Configuración → Reuniones. Puedes agregar, editar y quitar nombres y URLs HTTPS después de instalar; no hay direcciones empresariales fijadas en el código.
- El enlace único de versiones anteriores se conserva como «Servicio existente», que puedes renombrar. Una instalación nueva empieza sin servicios predefinidos.
- El color personalizado del widget incluye una muestra y un selector visual; también permite introducir el hexadecimal y previsualizarlo.
- Se conserva la integración con Outlook clásico, los calendarios y los ajustes personales.

## Cómo actualizar

Desde la versión 0.1.0.7, abre Configuración → General → Actualizar… y confirma la descarga e instalación. Después de actualizar, el botón se llama «Buscar actualizaciones» y todo se muestra dentro de General.

Para configurar tus servicios, abre Reuniones → Agregar servicio, escribe un nombre y su URL para programar reuniones, y pulsa Guardar. Elegir un servicio abre esa página en el navegador configurado; no envía invitaciones ni crea citas automáticamente.

El SHA-256 verifica la integridad respecto a los metadatos publicados en GitHub; no sustituye una firma digital independiente. La distribución sigue sin firma Authenticode.

## Historial

### Versión 0.1.0.7 — Actualizaciones bajo demanda

- Nuevo botón «Actualizar…» en Configuración → General.
- Consulta la versión marcada como Latest en este repositorio únicamente al pulsar el botón. No hay búsquedas ni descargas automáticas.
- Compara las versiones numéricamente, muestra las novedades y permite descargar e instalar una versión posterior.
- Verifica origen, tamaño, SHA-256 y versión del instalador antes de ejecutarlo; rechaza versiones anteriores y descargas incompletas.
- Conserva los calendarios y la configuración, y vuelve a abrir MeetingBar después de actualizar.
- La agenda y la ventana de configuración muestran la versión instalada en pequeño.

## Instalación

Descarga **MeetingBar-Windows-v0.1.0.7-Setup.exe** y ejecútalo. La versión 0.1.0.6 necesita esta primera instalación para incorporar el actualizador. A partir de la 0.1.0.7, usa Configuración → General → Actualizar…

El manifiesto `update.json` se publica junto al instalador en cada versión. El SHA-256 verifica la integridad respecto a los metadatos de GitHub; no sustituye una firma digital independiente. La distribución sigue sin firma Authenticode.

## Cambios anteriores

### Versión 0.1.0.6 — Outlook clásico y español

- «Abrir calendario» usa Outlook clásico directamente y abre el calendario de la fuente del evento.
- Traducción al español de la agenda, los detalles, los recordatorios, los ajustes y el instalador.
- Fechas, horas, estados vacíos, ayudas de accesibilidad y nombres predeterminados coherentes en español.
- Se conservan los ajustes existentes y se respeta la elección de acceso automático a las reuniones.
- La sincronización de Outlook conserva su lógica y el formato regional utilizado para consultar las citas.

Versión optimizada con instalador para Windows de 64 bits.

## Descargar e instalar

Descarga **MeetingBar-Windows-v0.1.0.6-Setup.exe** y ejecútalo. Instala MeetingBar para tu usuario, incluye las dependencias de la ventana de ajustes, crea un acceso en Inicio y permite desinstalar desde Windows. No requiere permisos de administrador.

Cierra MeetingBar y su ventana de ajustes antes de actualizar. Se conserva la configuración existente, incluida tu elección de acceso automático a las reuniones. Esta función está desactivada de forma predeterminada en instalaciones nuevas. La configuración anterior se respalda en `%APPDATA%\MeetingBar\settings.json.before-install.bak`. El inicio automático se habilita en instalaciones nuevas y respeta la preferencia existente al actualizar.

También se incluye el ZIP portable y `SHA256SUMS.txt`.

## Selección de monitor

- Configuración → Widget → Monitor permite seleccionar la pantalla donde aparece el widget.
- Recuerda la selección; si se desconecta usa la principal y vuelve al reconectarla.
- La agenda se abre en la pantalla elegida. El ocultamiento por pantalla completa considera ese monitor.

## Agenda compacta

- La barra prioriza citas con hora sobre feriados y recordatorios de todo el día.
- Mantiene la cuenta regresiva si faltan 24 horas o más; a menos de 24 horas muestra la hora de inicio en formato de 12 horas (AM/PM). Durante la reunión muestra el tiempo restante.
- La agenda separa Todo el día de Citas con hora.
- Configuración → Eventos permite incluir o excluir los eventos de todo el día de la barra sin ocultarlos de la agenda.
- Si no hay citas próximas, muestra ese estado y debajo el próximo evento de todo el día, si existe.

- Recuperación del widget al redibujarse la barra de Windows y eliminación de recargas visuales innecesarias al aplicar ajustes.

## Mejoras conservadas

- Menor memoria residente: agenda bajo demanda, diseño en caché y renderizado del widget sin contexto GPU permanente.
- Próximo evento independiente del intervalo visible de la agenda, con búsqueda hasta 90 días adelante.
- Títulos largos en dos líneas con letra más pequeña.
- Se eliminó el cuadro blanco que aparecía al poner el mouse encima del título.
- Guardado de ajustes que conserva cambios externos no editados.
- Identidad por ocurrencia para reuniones recurrentes de Outlook y conservación de eventos previos si falla la lectura.
- Validación de protocolos y detección más segura de enlaces de reunión.

## Requisitos y comprobaciones

Windows 10/11 x64, .NET Framework 4.7.2 o posterior. Para leer calendarios locales se necesita Outlook clásico.

La automatización compila desde el código del fork, ejecuta las pruebas y verifica una instalación y desinstalación silenciosas antes de publicar. Los ejecutables no tienen firma digital.

Versión optimizada con instalador para Windows de 64 bits.

## Descargar e instalar

Descargá **MeetingBar-Windows-v0.1.0.5-Setup.exe** y ejecutalo. Instala MeetingBar para tu usuario, incluye las dependencias de la ventana de ajustes, crea un acceso en Inicio y permite desinstalar desde Windows. No requiere permisos de administrador.

Cerrá MeetingBar y su ventana de ajustes antes de actualizar. Se conserva la configuración existente y se desactiva Auto-join durante la instalación. La configuración anterior se respalda en `%APPDATA%\MeetingBar\settings.json.before-install.bak`. El inicio automático se habilita en instalaciones nuevas y respeta la preferencia existente al actualizar.

También se incluye el ZIP portable y `SHA256SUMS.txt`.

## Selección de monitor

- Settings → Widget → Monitor permite seleccionar la pantalla donde aparece el widget.
- Recuerda la selección; si se desconecta usa la principal y vuelve al reconectarla.
- La agenda se abre en la pantalla elegida. El ocultamiento por pantalla completa considera ese monitor.

## Agenda compacta

- La barra prioriza citas con hora sobre feriados y recordatorios de todo el día.
- Mantiene la cuenta regresiva si faltan 24 horas o más; a menos de 24 horas muestra la hora de inicio en formato de 12 horas (AM/PM). Durante la reunión muestra el tiempo restante.
- La agenda separa Todo el día de Citas con hora.
- Settings → Events permite incluir o excluir los eventos de todo el día de la barra sin ocultarlos de la agenda.
- Si no hay citas próximas, muestra ese estado y debajo el próximo evento de todo el día, si existe.

- Recuperación del widget al redibujarse la barra de Windows y eliminación de recargas visuales innecesarias al aplicar ajustes.

## Mejoras conservadas

- Menor memoria residente: agenda bajo demanda, diseño en caché y renderizado del widget sin contexto GPU permanente.
- Próximo evento independiente del intervalo visible de la agenda, con búsqueda hasta 90 días adelante.
- Títulos largos en dos líneas con letra más pequeña.
- Se eliminó el cuadro blanco que aparecía al poner el mouse encima del título.
- Guardado de ajustes que conserva cambios externos no editados.
- Identidad por ocurrencia para reuniones recurrentes de Outlook y conservación de eventos previos si falla la lectura.
- Validación de protocolos y detección más segura de enlaces de reunión.

## Requisitos y comprobaciones

Windows 10/11 x64, .NET Framework 4.7.2 o posterior. Para leer calendarios locales se necesita Outlook clásico.

La automatización compila desde el código del fork, ejecuta las pruebas y verifica una instalación y desinstalación silenciosas antes de publicar. Los ejecutables no tienen firma digital.
