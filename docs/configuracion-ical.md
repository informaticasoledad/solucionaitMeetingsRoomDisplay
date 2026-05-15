# Configuración del proveedor iCal

El proveedor iCal permite leer eventos desde cualquier fuente que exponga un archivo `.ics` vía HTTP. Cada sala tiene su propia URL de calendario.

## Cómo funciona

- El **Calendar ID** de cada sala es directamente la URL del archivo `.ics`.
- No requiere credenciales en Ajustes.
- Es **solo lectura** en el calendario remoto. Las reservas rápidas desde el kiosko se guardan localmente en la base de datos.

## Paso 1: Obtener la URL .ics del calendario

### Google Calendar

1. Ve a [calendar.google.com](https://calendar.google.com).
2. En la barra lateral izquierda, coloca el ratón sobre el calendario.
3. Haz clic en `⋯` > **Configuración y uso compartido**.
4. Desplázate hasta **Integrar calendario**.
5. Copia la **Dirección secreta en formato iCal**:

```
https://calendar.google.com/calendar/ical/XXXXXXXXXXXX/basic.ics
```

### Microsoft Outlook / Office 365

1. Ve a [outlook.office.com/calendar](https://outlook.office.com/calendar).
2. **Configuración** > **Ver toda la configuración de Outlook** > **Calendario** > **Calendarios compartidos**.
3. **Publicar un calendario** y copia el enlace ICS.

### Apple iCloud

1. App Calendario en macOS > selecciona el calendario > **Editar** > **Compartir públicamente**.
2. Copia el enlace.

### Zoho Calendar

1. [calendar.zoho.com](https://calendar.zoho.com) > comparte el calendario > pestaña **Exportar**.
2. Activa **Enlace privado ICS** y copia la URL.

## Paso 2: Configurar la sala

1. Ve a **Salas** en el panel de administración.
2. Crea o edita una sala.
3. Selecciona **iCal (suscripción .ics)** como proveedor.
4. En el campo **URL del calendario (.ics)** introduce la URL completa del archivo `.ics`:

```
https://calendar.google.com/calendar/ical/XXXXXXXXXXXX/basic.ics
```

5. Guarda.

Cada sala tiene su propia URL — sala A tendrá `.../ical/AAAA/basic.ics`, sala B `.../ical/BBBB/basic.ics`, etc.

## Limitaciones

- **Solo lectura**: no se pueden crear ni modificar eventos en el calendario remoto.
- **Reservas locales**: las reservas del kiosko se guardan en la BD local y se muestran junto con los eventos del `.ics`.
- **Latencia**: los archivos `.ics` se refrescan cada 5-60 min según el proveedor.

## Solución de problemas

| Error                              | Solución posible                                                  |
|------------------------------------|-------------------------------------------------------------------|
| Error HTTP 404                     | La URL ya no es válida. Revisa la URL en el navegador.            |
| Error HTTP 403                     | La URL no es accesible. Usa una URL secreta/privada.              |
| Eventos no aparecen                | Abre la URL en el navegador para verificar que devuelve `.ics`.   |
| Eventos desactualizados            | Google y otros tardan horas en refrescar el `.ics`.               |
