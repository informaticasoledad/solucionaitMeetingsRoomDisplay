# Documentación Funcional — Meeting Room Display

## 1. Visión General del Sistema

Meeting Room Display es una solución integral para la gestión visual de salas de reuniones en entornos corporativos. Cada sala física dispone de una tablet en la puerta que actúa como display informativo en modo kiosko, indicando en tiempo real la disponibilidad de la sala consultando Google Calendar. Un panel de administración web permite la configuración centralizada de todas las salas.

## 2. Roles de Usuario

| Rol | Descripción | Acceso |
|-----|-------------|--------|
| **Administrador** | Gestiona salas y configuración general | Panel de administración web completo |
| **Usuario de sala** | Cualquier persona que consulta la tablet | Vista kiosko (solo lectura + reserva rápida) |

## 3. Historias de Usuario

### HU-01: Ver disponibilidad de una sala desde la tablet
**Como** usuario que se acerca a una sala de reuniones  
**Quiero** ver en la tablet si la sala está libre u ocupada  
**Para** saber si puedo usarla sin necesidad de abrir el calendario

**Criterios de aceptación:**
- La pantalla muestra un reloj digital grande con la hora actual
- El color de fondo/borde indica el estado: verde (libre), amarillo (próximamente ocupada), rojo (ocupada)
- Si hay reunión en curso, se muestra el título y horario
- Se muestra la próxima reunión programada si existe
- La información se actualiza automáticamente cada 30 segundos

### HU-02: Recibir alerta de sala a punto de ocuparse
**Como** usuario usando una sala libre  
**Quiero** que la tablet me avise cuando una reunión está a punto de empezar  
**Para** desalojar la sala a tiempo

**Criterios de aceptación:**
- Cuando falta menos de 15 minutos para la próxima reunión, el indicador cambia a amarillo
- Se muestra el título de la reunión entrante y la hora de inicio

### HU-03: Hacer una reserva rápida de emergencia
**Como** usuario que necesita una sala inmediatamente  
**Quiero** poder reservar la sala desde la propia tablet con un solo toque  
**Para** no tener que abrir Google Calendar en mi dispositivo

**Criterios de aceptación:**
- El botón "Reservar Ahora" solo aparece si la sala está libre o en estado "próximamente"
- La reserva se crea como evento de 15 minutos en Google Calendar
- Tras reservar, la tablet refleja el nuevo estado inmediatamente

### HU-04: Gestionar el catálogo de salas
**Como** administrador  
**Quiero** crear, editar y eliminar salas desde un panel web  
**Para** mantener actualizado el inventario de salas disponibles

**Criterios de aceptación:**
- Formulario con campos: Nombre, Capacidad, Google Calendar ID
- Listado con todas las salas y botones de editar/eliminar
- Validación de campos obligatorios

### HU-05: Ver dashboard con el estado de todas las salas
**Como** administrador o recepcionista  
**Quiero** ver un panel con el estado en tiempo real de todas las salas  
**Para** dirigir a los visitantes a la sala correcta

**Criterios de aceptación:**
- Grid de tarjetas, una por sala
- Cada tarjeta muestra: nombre, capacidad, estado (con color), reunión actual o próxima
- Los datos se actualizan automáticamente cada 30 segundos

### HU-06: Configurar la conexión con Google Calendar
**Como** administrador  
**Quiero** cargar las credenciales de la cuenta de servicio de Google  
**Para** que el sistema pueda leer y escribir en los calendarios de las salas

**Criterios de aceptación:**
- Textarea para pegar el contenido del archivo JSON de la service account
- Botón para guardar las credenciales
- Botón para forzar una sincronización manual
- Feedback visual de éxito o error tras cada acción

## 4. Flujos de Trabajo

### 4.1 Flujo de visualización en kiosko

```
Tablet enciende
    ↓
Carga URL /kiosk/{roomId}
    ↓
GET /api/status/{roomId}
    ↓
¿Respuesta exitosa?
  ├── Sí → Renderizar vista según estado
  │         ├── Free → Fondo oscuro, badge verde
  │         ├── BusySoon → Fondo amarillo oscuro, badge amarillo
  │         └── Occupied → Fondo rojo oscuro, badge rojo
  └── No → Mostrar "Cargando…" y reintentar
    ↓
Cada 30 segundos → Volver a consultar /api/status/{roomId}
Cada 1 segundo → Actualizar reloj
```

### 4.2 Flujo de reserva rápida

```
Usuario presiona "Reservar Ahora"
    ↓
POST /api/reservations/quick { roomId, durationMinutes: 15 }
    ↓
Backend verifica que la sala no esté ocupada
    ↓
Backend crea evento en el calendario de la sala según su proveedor configurado
    ↓
Backend actualiza eventos locales de la sala
    ↓
Respuesta exitosa → Recargar estado de la sala
Respuesta error → Mostrar estado actual (sin cambios)
```

### 4.3 Flujo de sincronización de calendarios

```
BackgroundService inicia
    ↓
Cada 60 segundos:
    ↓
Obtener todas las salas de la BD
    ↓
Para cada sala:
    ├── GET Google Calendar API (eventos próximos 7 días)
    ├── Mapear a MeetingEvent[]
    ├── Actualizar Room.CurrentEvents
    └── Guardar en BD
    ↓
Log de sincronización completada
```

## 5. Matriz de Estados de Sala

| Estado | Significado | Color UI | Condición |
|--------|-------------|----------|-----------|
| **Free** | Sala disponible sin reservas próximas | Verde | Sin reunión actual Y siguiente reunión a >15 min |
| **BusySoon** | Sala libre pero con reserva inminente | Amarillo | Sin reunión actual Y siguiente reunión a ≤15 min |
| **Occupied** | Sala en uso | Rojo | Hay una reunión en curso (now entre start y end) |

## 6. Requisitos No Funcionales

| Requisito | Descripción |
|-----------|-------------|
| **Disponibilidad** | La vista kiosko debe funcionar incluso si Google Calendar no responde (muestra último estado conocido) |
| **Latencia** | La sincronización de estado no debe superar los 60 segundos de desfase |
| **Portabilidad** | El sistema completo se despliega con un solo `docker compose up` |
| **Tamaño de imagen** | Imagen Docker final inferior a 150 MB |
| **Navegador kiosko** | Compatible con navegadores basados en Chromium en modo kiosko (Android, ChromeOS, Linux) |
| **Idioma** | Interfaz en español |
