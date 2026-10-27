Versión optimizada con instalador para Windows de 64 bits.

## Descargar e instalar

Descargá **MeetingBar-Windows-v0.1.0.3-Setup.exe** y ejecutalo. Instala MeetingBar para tu usuario, incluye las dependencias de la ventana de ajustes, crea un acceso en Inicio y permite desinstalar desde Windows. No requiere permisos de administrador.

Cerrá MeetingBar y su ventana de ajustes antes de actualizar. Se conserva la configuración existente y se desactiva Auto-join durante la instalación. La configuración anterior se respalda en `%APPDATA%\MeetingBar\settings.json.before-install.bak`. El inicio automático se habilita en instalaciones nuevas y respeta la preferencia existente al actualizar.

También se incluye el ZIP portable y `SHA256SUMS.txt`.

## Mejoras

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
