# Configuración de Google Calendar

Este documento explica paso a paso cómo configurar la conexión con Google Calendar para que el sistema pueda leer y crear eventos en los calendarios de las salas.

## Requisitos previos

- Una cuenta de Google Workspace con permisos de administrador.
- Acceso a la [Google Cloud Console](https://console.cloud.google.com/).

## Paso 1: Crear un proyecto en Google Cloud

1. Ve a [console.cloud.google.com](https://console.cloud.google.com/).
2. Crea un proyecto nuevo o selecciona uno existente.
3. Ve a **IAM y administración** > **Crear proyecto**.
4. Asigna un nombre (ej: `MeetingRoomDisplay`) y haz clic en **Crear**.

## Paso 2: Habilitar la API de Google Calendar

1. En el menú lateral, ve a **APIs y servicios** > **Biblioteca**.
2. Busca **Google Calendar API**.
3. Haz clic en **Habilitar**.

## Paso 3: Crear una cuenta de servicio (Service Account)

1. Ve a **APIs y servicios** > **Credenciales**.
2. Haz clic en **Crear credenciales** > **Cuenta de servicio**.
3. Completa los campos:
   - **Nombre**: `meeting-room-display` (o el que prefieras).
   - **ID**: se genera automáticamente. El email tendrá el formato `meeting-room-display@<proyecto>.iam.gserviceaccount.com`.
   - **Descripción**: opcional.
4. Haz clic en **Crear y continuar**.
5. Asigna el rol `Calendarios > Eventos de Calendario` (puedes buscar "Calendar Events").
6. Haz clic en **Listo**.

## Paso 4: Delegación de dominio (Domain-Wide Delegation)

Si usas Google Workspace y necesitas acceder a los calendarios de otros usuarios (las salas), debes configurar delegación de dominio:

1. Haz clic sobre la cuenta de servicio recién creada.
2. En la parte superior, haz clic en **Editar**.
3. Despliega la sección **Delegación de dominio en todo Google Workspace**.
4. Actívala y añade los siguientes **scopes de API**:

```
https://www.googleapis.com/auth/calendar.events
https://www.googleapis.com/auth/calendar.readonly
```

5. Guarda los cambios.

Ahora ve al **Panel de administración de Google Workspace**:

1. Ve a **Seguridad** > **Controles de acceso y datos** > **Controles de API**.
2. En **Delegación de dominio en toda la organización**, haz clic en **Administrar delegación de dominio**.
3. Haz clic en **Añadir nuevo**.
4. Introduce el **ID de cliente** de la cuenta de servicio (lo encuentras en la página de la cuenta de servicio en Google Cloud Console).
5. Añade los mismos scopes de API:

```
https://www.googleapis.com/auth/calendar.events
https://www.googleapis.com/auth/calendar.readonly
```

6. Haz clic en **Autorizar**.

## Paso 5: Generar la clave JSON

1. En **Google Cloud Console** > **APIs y servicios** > **Credenciales**.
2. Busca la cuenta de servicio que creaste.
3. Haz clic en el email de la cuenta de servicio para ver sus detalles.
4. Ve a la pestaña **Claves**.
5. Haz clic en **Agregar clave** > **Crear clave nueva**.
6. Selecciona formato **JSON**.
7. Se descargará un archivo `.json`. **Guárdalo en un lugar seguro**, contiene la clave privada.

El archivo tendrá un aspecto similar a:

```json
{
  "type": "service_account",
  "project_id": "meeting-room-display",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEv...\n-----END PRIVATE KEY-----\n",
  "client_email": "meeting-room-display@meeting-room-display.iam.gserviceaccount.com",
  "client_id": "1234567890",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/..."
}
```

## Paso 6: Obtener el Calendar ID de las salas

Cada sala de Google Workspace tiene un **Calendar ID**, que es su dirección de email.

1. Ve a [calendar.google.com](https://calendar.google.com/).
2. En el panel izquierdo, busca la sala en la sección **Otras salas** o **Mis calendarios**.
3. Haz clic en los tres puntos junto al nombre de la sala > **Configuración y uso compartido**.
4. En la sección **Integrar calendario**, copia el **ID de calendario** (tiene formato de email: `sala.atlantico@miempresa.com`).

## Paso 7: Configurar la sala en la aplicación

1. Ve al panel de administración en `http://localhost:8080/admin`.
2. Ve a **Configuración**.
3. Selecciona el proveedor **Google Calendar**.
4. Pega el contenido completo del archivo JSON de la cuenta de servicio en el campo de texto.
5. Haz clic en **Guardar Credenciales**.

Ahora, al crear o editar una sala:

1. Ve a **Salas** > **Nueva Sala**.
2. Introduce el código, nombre, capacidad y tipo de reloj.
3. En **Proveedor**, selecciona `Google`.
4. En **Calendar ID**, pega el ID de calendario de la sala (el email que copiaste en el paso 6).
5. Guarda.

## Paso 8: Verificar la conexión

1. Ve a **Configuración** > haz clic en **Sincronizar Ahora**.
2. Ve al **Dashboard** — deberías ver el estado de la sala actualizado con los eventos del calendario.
3. Abre el modo **Kiosko** desde el enlace en la lista de salas — los eventos deberían aparecer en la timeline.

## Solución de problemas

### "Provider not configured"
Asegúrate de haber pegado el JSON de la cuenta de servicio en **Configuración > Google** y haber hecho clic en **Guardar Credenciales**.

### "Calendar not found"
Verifica que el **Calendar ID** de la sala sea correcto y que la cuenta de servicio tenga acceso a ese calendario. Si usas Google Workspace, asegúrate de haber configurado la delegación de dominio (Paso 4).

### Error 403 / Forbidden
La cuenta de servicio no tiene permisos sobre el calendario. Comprueba que:
1. La API de Calendar esté habilitada.
2. La delegación de dominio esté configurada correctamente.
3. El calendar ID pertenezca a un usuario de tu dominio de Workspace.

### La clave privada contiene saltos de línea
Al pegar el JSON en la aplicación, asegúrate de que los saltos de línea (`\n`) dentro del campo `private_key` se conserven exactamente como vienen en el archivo descargado. El sistema los interpreta correctamente.
