# Configuración del proveedor CalDAV

CalDAV es un protocolo estándar abierto para acceder a calendarios de forma remota. Es compatible con servidores como **Nextcloud**, **ownCloud**, **Apple iCloud**, **Baikal**, **Radicale**, **Synology Calendar**, **mailbox.org**, entre otros.

## Requisitos previos

- Un servidor CalDAV accesible por HTTP/HTTPS.
- Usuario y contraseña con permisos de lectura y escritura sobre el calendario.
- La URL base del servidor CalDAV.

## Paso 1: Obtener la URL base del servidor

La URL base es el endpoint raíz del servidor CalDAV. Dependiendo del servidor:

| Proveedor           | URL base típica                           |
|---------------------|-------------------------------------------|
| Nextcloud           | `https://tudominio.com/remote.php/dav`   |
| ownCloud            | `https://tudominio.com/remote.php/dav`   |
| iCloud              | `https://caldav.icloud.com`              |
| Baikal              | `https://tudominio.com/dav.php`          |
| Radicale            | `https://tudominio.com`                  |
| Synology Calendar   | `https://tudominio.com:5001/caldav`      |
| mailbox.org         | `https://dav.mailbox.org`                |
| GMX / WEB.DE        | `https://caldav.gmx.net`                 |

Si tienes dudas, revisa la documentación de tu servidor. En muchos casos puedes encontrarla en los **ajustes de sincronización** del calendario.

## Paso 2: Obtener el Calendar ID

Para Nextcloud, ownCloud y similares, la URL del calendario suele ser:

```
https://tudominio.com/remote.php/dav/calendars/usuario/calendario-personal
```

En ese caso:
- **URL base**: `https://tudominio.com/remote.php/dav`
- **Calendar ID**: `calendars/usuario/calendario-personal`

El Calendar ID es **la ruta restante después de la URL base**. 

### Ejemplo Nextcloud

Si la URL completa del calendario es:

```
https://cloud.miempresa.com/remote.php/dav/calendars/admin/sala-reuniones
```

La configuración sería:
- URL base: `https://cloud.miempresa.com/remote.php/dav`
- Calendar ID: `calendars/admin/sala-reuniones`

### Ejemplo iCloud

Para cuentas de iCloud, la URL base es `https://caldav.icloud.com` y el Calendar ID suele ser el número de identificación del calendario (puedes encontrarlo en los detalles del calendario en la app Calendario de macOS).

## Paso 3: Credenciales de acceso

Para la autenticación se usa HTTP Basic Auth con usuario y contraseña.

### Usuarios estándar

En la mayoría de servidores (Nextcloud, ownCloud, Baikal, Radicale), el usuario y contraseña son las credenciales de la cuenta.

### Contraseña de aplicación (app password)

Algunos servicios requieren contraseñas de aplicación en lugar de la contraseña principal:

- **Nextcloud**: Ve a **Ajustes > Seguridad > Contraseñas de aplicación** y genera una nueva.
- **iCloud**: Ve a [appleid.apple.com](https://appleid.apple.com) > Inicio de sesión y seguridad > Contraseñas de aplicación.
- **mailbox.org**: Genera una contraseña de aplicación desde los ajustes de seguridad de tu cuenta.

## Paso 4: Formato del JSON de credenciales

En la sección **Ajustes > Proveedores de calendario** del panel de administración, selecciona **CalDAV** como proveedor y pega un JSON con el siguiente formato:

```json
{
  "url": "https://cloud.miempresa.com/remote.php/dav",
  "username": "admin",
  "password": "tu-contrasena-o-app-password"
}
```

## Paso 5: Configurar la sala

1. Ve a **Salas** en el panel de administración.
2. Crea o edita una sala.
3. Selecciona **CalDAV (Nextcloud, iCloud, …)** como proveedor.
4. Introduce el Calendar ID (ej: `calendars/admin/sala-reuniones`).
5. Guarda la sala.
6. Ve a **Ajustes > Proveedores de calendario** y pega el JSON de credenciales indicado arriba.

## Verificación

Una vez configurado, puedes verificar el funcionamiento:

1. Ve a **Salas > Kiosko** junto a la sala configurada.
2. Deberías ver los eventos del calendario CalDAV cargados en el panel lateral.
3. Prueba a hacer una reserva rápida desde el kiosko para confirmar que la escritura funciona.

## Solución de problemas

| Error                              | Solución posible                                                   |
|------------------------------------|--------------------------------------------------------------------|
| `CalDAV Calendar not configured`   | Ve a Ajustes y pega el JSON de credenciales en el proveedor CalDAV. |
| Error HTTP 401 (No autorizado)     | Revisa usuario y contraseña. Prueba con una contraseña de aplicación. |
| Error HTTP 404 (No encontrado)     | Verifica la URL base y el Calendar ID.                             |
| Error HTTP 405 (Método no válido)  | Asegúrate de que la URL base apunta al endpoint DAV correcto.      |
| Los eventos no aparecen            | Confirma que el Calendar ID es correcto y empieza desde la raíz DAV. |
| No se crean eventos                | Verifica que el usuario tenga permisos de escritura en el calendario. |
| Error SSL / certificado            | Asegúrate de que el certificado SSL del servidor es válido.        |

## Compatibilidad con Zimbra

Zimbra también expone un endpoint CalDAV. La URL base suele ser:

```
https://mail.miempresa.com/dav
```

Y el Calendar ID suele ser el nombre del calendario (ej: `Calendario`). Si tienes problemas, usa la URL completa como Calendar ID:

```
https://mail.miempresa.com/dav/username/Calendario
```

Con la URL base `https://mail.miempresa.com/dav` y el Calendar ID `username/Calendario`.

## Configurar Google Calendar mediante CalDAV

Aunque Google Calendar tiene su propio proveedor nativo en esta aplicación (recomendado), también es posible conectarse a Google Calendar usando el protocolo CalDAV. Esto es útil si prefieres no usar cuentas de servicio de Google Cloud.

### Requisitos previos

- Una cuenta de Google personal o de Google Workspace.
- Verificación en dos pasos activada en la cuenta de Google.
- Contraseña de aplicación generada para "CalDAV".

### Paso 1: Activar verificación en dos pasos

1. Ve a [myaccount.google.com/security](https://myaccount.google.com/security).
2. En **Iniciar sesión en Google**, haz clic en **Verificación en dos pasos**.
3. Sigue los pasos para activarla si no lo está ya.

### Paso 2: Generar una contraseña de aplicación

1. Ve a [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords).
2. En **Seleccionar aplicación**, elige **Otra (nombre personalizado)**.
3. Escribe un nombre descriptivo como `MeetingRoomDisplay CalDAV`.
4. Haz clic en **Generar**.
5. Copia la contraseña de 16 caracteres que aparece. La necesitarás en el paso 4.

### Paso 3: Obtener el Calendar ID

El Calendar ID de Google es el email asociado al calendario:

- **Calendario personal (principal)**: tu dirección de Gmail (ej: `usuario@gmail.com`).
- **Calendario secundario**: el email del calendario, que puedes encontrar en Google Calendar:
  1. Ve a [calendar.google.com](https://calendar.google.com).
  2. En la barra lateral izquierda, coloca el ratón sobre el calendario que quieras usar.
  3. Haz clic en los tres puntos `⋯` > **Configuración y uso compartido**.
  4. En **Integrar calendario**, copia la dirección del **ID del calendario** (tiene formato de email).

### Paso 4: Configurar el JSON de credenciales

La URL base de CalDAV para Google Calendar es:

```
https://apidata.googleusercontent.com/caldav/v2
```

El JSON de credenciales debe ser:

```json
{
  "url": "https://apidata.googleusercontent.com/caldav/v2",
  "username": "tu-correo@gmail.com",
  "password": "xxxx-xxxx-xxxx-xxxx"
}
```

Donde:
- `username`: tu dirección de Gmail o, si usas Google Workspace, `tu-usuario@tu-dominio.com`.
- `password`: la contraseña de aplicación generada en el paso 2.

### Paso 5: Configurar la sala

1. Ve a **Salas** en el panel de administración.
2. Crea o edita una sala.
3. Selecciona **CalDAV (Nextcloud, iCloud, …)** como proveedor.
4. En **Calendar ID**, introduce el email del calendario de Google (ej: `usuario@gmail.com` o `calendario-secundario@group.calendar.google.com`).
5. Guarda la sala.
6. Ve a **Ajustes > Proveedores de calendario**, selecciona **CalDAV** y pega el JSON de credenciales.

### Notas importantes

- Google Calendar vía CalDAV es **solo lectura** para la creación de eventos por parte de esta aplicación. Google restringe la escritura vía CalDAV a clientes propios. Para tener funcionalidad completa (lectura y escritura), usa el proveedor nativo **Google Calendar** de esta aplicación.
- Si solo necesitas consultar eventos del calendario y no crear reservas desde el kiosko, CalDAV con Google Calendar funciona correctamente.
- El endpoint CalDAV de Google está oficialmente en modo mantenimiento y podría dejar de funcionar en el futuro. Se recomienda migrar eventualmente al proveedor nativo de Google Calendar.

### Solución de problemas específicos de Google Calendar

| Error                              | Solución posible                                                   |
|------------------------------------|--------------------------------------------------------------------|
| Error HTTP 401 (No autorizado)     | Verifica que la contraseña de aplicación sea correcta. Genera una nueva si es necesario. |
| Error HTTP 403 (Prohibido)         | Puede indicar que el Calendar ID no existe o la cuenta no tiene acceso a ese calendario. |
| No aparecen eventos                | Asegúrate de que el calendario es público o que la cuenta tiene permisos de lectura. |
| No se crean eventos                | Google bloquea la creación de eventos vía CalDAV desde clientes de terceros. Usa el proveedor nativo Google Calendar. |
| La contraseña de aplicación expiró | Las contraseñas de aplicación de Google caducan si no se usan. Genera una nueva desde [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords). |
