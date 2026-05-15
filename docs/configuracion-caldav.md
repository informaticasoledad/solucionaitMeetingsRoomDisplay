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
