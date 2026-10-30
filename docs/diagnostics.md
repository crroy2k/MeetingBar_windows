# Diagnóstico de cierres

Desde la versión 0.1.0.10, los registros locales se encuentran en `%APPDATA%\MeetingBar`:

- `meetingbar.log`: inicios, cierres solicitados, cambios de energía y errores. Cada entrada del proceso principal incluye hora local con zona, PID e identificador de ejecución.
- `meetingbar.log.1`, `.2` y `.3`: historial rotado. El registro actual rota al alcanzar aproximadamente 1 MiB y conserva tres archivos anteriores. Si existe un archivo `.old` de versiones anteriores, se conserva.
- `diagnostic-session.json`: versión, PID, inicio, última actividad del proceso y de la interfaz, intención de actualizar y resultado de cierre.
- `diagnostic-session.json.previous`: marcador de la ejecución anterior, conservado al iniciar una nueva.

## Qué se puede identificar

- `tray-menu`: el usuario eligió **Salir** en la bandeja.
- `ipc-quit`: llegó una orden de cierre por el canal local. Una entrada anterior registra el PID del cliente que envió la orden.
- `startup-quit`: la aplicación se inició con una orden de cierre.
- `windows-logoff` o `windows-shutdown`: Windows notificó el cierre de sesión o del sistema. Si lo cancela, también se registra y se restablece el estado de ejecución.
- `update-installer`: se solicitó una instalación desde Ajustes; Windows puede cerrar el proceso para reemplazar los archivos. La intención lleva su propia fecha y no se confunde con un cierre confirmado.
- `fatal`: se capturó una excepción fatal, con su tipo, HRESULT, métodos de la pila y excepciones internas.
- `clean-exit`: se completó la salida y limpieza de WPF, con su código y motivo cuando se conoce.
- `process-exit-without-cleanup`: se observó la terminación del proceso sin completar la salida habitual.
- `ended without confirmed clean exit`: al iniciar se encontró una ejecución anterior que no dejó confirmación de salida limpia.

Cada minuto se guarda una señal de actividad del proceso. La interfaz actualiza su propia señal en memoria con su temporizador existente, que queda reflejada en ese archivo. Una señal del proceso que avanza mientras la de la interfaz permanece detenida puede ayudar a investigar un bloqueo de la interfaz. No se realizan consultas adicionales al calendario ni a Internet.

Un cierre forzado desde el Administrador de tareas, una caída nativa o un corte de energía pueden impedir cualquier registro en ese instante. En esos casos se detecta la ausencia de salida confirmada al volver a abrir MeetingBar, pero el registro por sí solo no prueba quién terminó el proceso ni la causa exacta. Los sucesos de Windows pueden aportar evidencia adicional. La mejora no puede reconstruir retrospectivamente cierres de versiones anteriores.

## Privacidad y tolerancia a errores

Los nuevos detalles técnicos de excepciones del proceso principal excluyen mensajes libres de las excepciones, `Data`, argumentos, rutas de archivos fuente y datos del calendario. Se registran tipos de excepción, códigos y nombres de métodos. No se generan volcados de memoria ni se envían registros a terceros.

Los fallos al escribir los registros no cierran la aplicación. La mejora no añade reinicios automáticos ni cambia el funcionamiento de Outlook, los recordatorios o las actualizaciones bajo demanda.
