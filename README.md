# Meeting Room Display

Sistema de gestión de salas de reuniones con integración multi-calendario (Google Calendar, Office 365, Zoho), vista kiosko para tablets y panel de administración web.

## Arquitectura

```
Clean Architecture (4 capas) + Angular Standalone Components + Docker Multi-stage

┌──────────────────────────────────────────────────┐
│                    Docker Alpine                  │
│  ┌─────────────┐  ┌────────────────────────────┐ │
│  │   Angular    │  │     ASP.NET Core 10 API     │ │
│  │  (Kiosko +   │  │  ┌──────────┐ ┌──────────┐ │ │
│  │   Admin)     │  │  │Controllers│ │Swagger   │ │ │
│  │              │  │  └─────┬─────┘ └──────────┘ │ │
│  │  Tailwind    │  │  ┌─────┴──────────────────┐ │ │
│  │  Signals     │  │  │   Application Layer    │ │ │
│  │              │  │  │   (Use Cases / DTOs)   │ │ │
│  └──────┬───────┘  │  └─────┬──────────────────┘ │ │
│         │          │  ┌─────┴──────────────────┐ │ │
│         │          │  │  Infrastructure Layer   │ │ │
│         │          │  │  (EF Core + Google API) │ │ │
│  wwwroot/ (static) │  └─────┬──────────────────┘ │ │
│         │          │  ┌─────┴──────────────────┐ │ │
│         └──────────┤  │   Core / Domain Layer  │ │ │
│                    │  │  (Entities, VO, DDD)   │ │ │
│                    │  └────────────────────────┘ │ │
│                    │         SQLite DB            │ │
│                    └────────────────────────────┘ │
└──────────────────────────────────────────────────┘
```

### Backend (.NET 10)

| Capa | Proyecto | Responsabilidad |
|------|----------|----------------|
| **Core** | `MeetingRoom.Core` | Entidades DDD (`Room`, `MeetingEvent`, `CalendarId`, `RoomStatus`), interfaces (`IRoomRepository`, `IGoogleCalendarService`) |
| **Application** | `MeetingRoom.Application` | Casos de uso (GetRooms, CreateRoom, GetRoomStatus, QuickReserve, SyncCalendars), DTOs |
| **Infrastructure** | `MeetingRoom.Infrastructure` | EF Core SQLite, `GoogleCalendarService`, `CalendarSyncService` (BackgroundService cada 60s) |
| **API** | `MeetingRoom.Api` | Controllers REST, middleware de errores, Swagger, sirve estáticos de Angular |

### Frontend (Angular 19)

| App | Ruta | Descripción |
|-----|------|-------------|
| **Kiosko** | `/kiosk/:roomId` | Vista tablet minimalista: fondo oscuro, borde verde/amarillo/rojo según estado, reloj en tiempo real, info de reuniones, botón "Reservar Ahora" |
| **Admin** | `/admin` | Panel con sidebar: Dashboard (estado en tiempo real de todas las salas), CRUD de Salas, Configuración (subir credenciales Google, forzar sync) |

## Configuración de Google Calendar

### 1. Crear cuenta de servicio en Google Cloud

1. Ve a [Google Cloud Console](https://console.cloud.google.com)
2. Crea un proyecto o selecciona uno existente
3. Ve a **APIs & Services > Library** y habilita **Google Calendar API**
4. Ve a **APIs & Services > Credentials > Create Credentials > Service Account**
5. Asigna un nombre (ej: `meeting-room-display`) y crea la cuenta
6. En la cuenta de servicio, ve a **Keys > Add Key > Create new key > JSON**
7. Descarga el archivo JSON

### 2. Compartir calendarios con la cuenta de servicio

Para cada sala que quieras monitorizar:

1. En Google Calendar, ve a la configuración del calendario de la sala
2. En **Share with specific people**, añade el email de la cuenta de servicio (aparece en el JSON como `client_email`)
3. Asigna permisos: **Make changes to events** (necesario para reserva rápida)

### 3. Configurar en la aplicación

1. Ve al panel de administración (`/admin`)
2. Navega a **Configuración**
3. Pega el contenido completo del archivo JSON descargado
4. Haz clic en **Guardar Credenciales**

Luego, en la sección **Salas**, crea cada sala con su **Calendar ID** correspondiente (visible en la configuración del calendario en Google Calendar).

## Compilación y Ejecución

### Requisitos previos

- [Docker](https://www.docker.com/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (para desarrollo local)
- [Node.js 22+](https://nodejs.org/) (para desarrollo local del frontend)

### Con Docker (recomendado para producción)

```bash
# Construir y ejecutar
docker compose up --build -d

# La aplicación estará disponible en:
# → Admin:     http://localhost:8080/#/admin
# → Kiosko:    http://localhost:8080/#/kiosk/{roomId}
# → Swagger:   http://localhost:8080/swagger
```

La imagen final pesa aproximadamente **120 MB** (Alpine multi-stage).

### Desarrollo local

**Backend:**

```bash
cd src/backend
dotnet restore
dotnet run --project src/API/MeetingRoom.Api
# API en http://localhost:5000
# Swagger en http://localhost:5000/swagger
```

**Frontend:**

```bash
cd src/frontend
npm install
npm start
# Dev server en http://localhost:4200 (con proxy a API)
```

**Tests:**

```bash
cd src/backend
dotnet test
```

## Endpoints de la API

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/rooms` | Listar todas las salas |
| `GET` | `/api/rooms/{id}` | Obtener sala por ID |
| `POST` | `/api/rooms` | Crear sala |
| `PUT` | `/api/rooms/{id}` | Actualizar sala |
| `DELETE` | `/api/rooms/{id}` | Eliminar sala |
| `GET` | `/api/status` | Dashboard: estado de todas las salas |
| `GET` | `/api/status/{id}` | Estado de una sala específica |
| `POST` | `/api/reservations/quick` | Reserva rápida (15 min por defecto) |
| `POST` | `/api/calendar/credentials` | Configurar credenciales Google |
| `POST` | `/api/calendar/sync` | Forzar sincronización manual |

## Diseño visual (Kiosko)

- **Fondo verde oscuro** → Sala libre
- **Fondo amarillo oscuro** → Sala libre pero reunión en &lt;15 min
- **Fondo rojo oscuro** → Sala ocupada

Cada vista muestra:
- Reloj digital grande (HH:MM) actualizado cada segundo
- Nombre de la sala y capacidad
- Badge de estado con indicador luminoso
- Reunión actual (si existe): título, horario
- Próxima reunión (si existe): título, horario
- Estado "disponible desde las HH:MM"
- Botón **Reservar Ahora** (solo visible si la sala no está ocupada)

## Stack tecnológico

| Componente | Tecnología |
|------------|------------|
| Backend | .NET 10, ASP.NET Core Web API |
| ORM | Entity Framework Core 10 + SQLite |
| Calendar API | Google.Apis.Calendar.v3 |
| Frontend | Angular 19, Standalone Components, Signals |
| Estilos | Tailwind CSS 4 |
| Tests | xUnit + Moq |
| Contenedor | Docker Multi-stage (Alpine) |
| Documentación | Swagger / OpenAPI |
