# Configuración del proveedor iCal

El proveedor iCal permite leer eventos desde cualquier fuente que exponga un archivo `.ics` vía HTTP. Esto incluye suscripciones a calendarios públicos de **Google Calendar**, **Office 365**, **Apple iCloud**, **Zoho Calendar**, **Zimbra**, o cualquier otro sistema que permita exportar un calendario en formato iCalendar.

## Requisitos previos

- Una URL pública o privada que devuelva un archivo `.ics` (iCalendar / RFC 5545).
- La URL debe ser accesible por HTTP desde el servidor donde se ejecuta la aplicación.

> **Nota**: iCal es solo lectura. Las reservas rápidas desde el kiosko se guardarán localmente en la base de datos en lugar de escribirse en el calendario remoto.

## Paso 1: Obtener la URL del calendario iCal

### Google Calendar

1. Ve a [calendar.google.com](https://calendar.google.com).
2. En la barra lateral izquierda, coloca el ratón sobre el calendario.
3. Haz clic en los tres puntos `⋯` > **Configuración y uso compartido**.
4. Desplázate hasta **Integrar calendario**.
5. Copia la URL en **Dirección secreta en formato iCal**:

```
https://calendar.google.com/calendar/ical/XXXXXXXXXXXX/basic.ics
```

> La dirección **pública** también funciona pero no se recomienda para calendarios con información sensible.

### Microsoft Outlook / Office 365

1. Ve a [outlook.office.com/calendar](https://outlook.office.com/calendar).
2. Ve a **Configuración** (icono de engranaje) > **Ver toda la configuración de Outlook**.
3. Ve a **Calendario** > **Calendarios compartidos**.
4. En **Publicar un calendario**, selecciona el calendario y permisos que quieras.
5. Haz clic en **Publicar** y copia el enlace **ICS**:

```
https://outlook.office365.com/owa/calendar/XXXXXXXXXXXXXXX/calendar.ics
```

### Apple iCloud

1. Ve a la app Calendario en macOS.
2. Selecciona el calendario de la barra lateral izquierda.
3. Haz clic en **Editar** > **Compartir públicamente**.
4. Copia el enlace generado:

```
https://publish.icloud.com/XXXXXXXXXXXX/Calendario
```

### Zoho Calendar

1. Ve a [calendar.zoho.com](https://calendar.zoho.com).
2. Coloca el ratón sobre el calendario y haz clic en el icono de compartir.
3. Selecciona la pestaña **Exportar**.
4. Activa **Enlace privado ICS** y copia la URL:

```
https://calendar.zoho.com/ical/XXXXXXXXXXXX
```

### Otros proveedores

Cualquier sistema que pueda exportar `.ics` vía URL funciona. Si tu servidor ya te provee una URL de tipo `webcal://`, reemplaza `webcal://` por `https://` o `http://` para usarla en esta aplicación.

## Paso 2: Configurar el JSON de credenciales

El proveedor iCal no necesita usuario ni contraseña. Solo requiere la URL del calendario. En la sección **Ajustes > Proveedores de calendario**, selecciona **iCal** y pega:

```json
{
  "url": "https://calendar.google.com/calendar/ical/XXXXXXXXXXXX/basic.ics"
}
```

## Paso 3: Configurar la sala

1. Ve a **Salas** en el panel de administración.
2. Crea o edita una sala.
3. Selecciona **iCal (suscripción .ics)** como proveedor.
4. Deja el Calendar ID vacío o introduce la misma URL del JSON de credenciales.
5. Guarda la sala.
6. Ve a **Ajustes > Proveedores de calendario** y pega el JSON de credenciales indicado.

## Verificación

1. Ve a **Salas > Kiosko** junto a la sala configurada.
2. Deberías ver los eventos del calendario iCal cargados en la línea de tiempo.
3. El refresco automático cada 30 segundos aplica también a iCal.

## Limitaciones

- **Solo lectura en el calendario remoto**: no se pueden crear, modificar ni eliminar eventos en el calendario de origen.
- **Reservas rápidas locales**: las reservas hechas desde el kiosko se guardan en la base de datos local y se muestran junto con los eventos del `.ics`, pero no se sincronizan de vuelta al calendario remoto.
- **Sin autenticación avanzada**: si la URL requiere Basic Auth, OAuth o cookies, el proveedor no podrá acceder. Para esos casos, usa el proveedor **CalDAV** en su lugar.
- **Latencia**: Google Calendar, iCloud y otros proveedores no actualizan el archivo `.ics` en tiempo real. Puede tardar entre 5 minutos y 24 horas dependiendo del servicio.

## Solución de problemas

| Error                              | Solución posible                                                              |
|------------------------------------|-------------------------------------------------------------------------------|
| `iCal subscription not configured` | Ve a Ajustes y pega el JSON de credenciales con la URL del calendario.        |
| Error HTTP 404 (No encontrado)     | La URL `.ics` ya no es válida o el calendario fue eliminado.                   |
| Error HTTP 403 (Prohibido)         | La URL no es accesible públicamente. Usa una URL privada con token secreto.   |
| Los eventos no aparecen            | Revisa que la URL devuelva contenido iCalendar válido. Ábrela en el navegador. |
| Eventos desactualizados            | Google Calendar y otros servicios pueden tardar horas en refrescar el `.ics`.  |
