# Documentación Técnica — Meeting Room Display

## 1. Stack Tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Runtime | .NET | 10.0 |
| Lenguaje backend | C# | 14 |
| Framework web | ASP.NET Core | 10.0 |
| ORM | Entity Framework Core | 10.0 |
| Base de datos | SQLite | 3 |
| Calendar API | Google.Apis.Calendar.v3 / Microsoft Graph / Zoho Calendar API | 1.69 / v1.0 / v1 |
| Frontend framework | Angular | 19 |
| Lenguaje frontend | TypeScript | 5.8 |
| CSS framework | Tailwind CSS | 4 |
| Testing backend | xUnit + Moq | 2.9 / 4.20 |
| Contenedor base | Alpine Linux | 3.21 |
| API documentation | Swashbuckle (Swagger) | 7.3 |

## 2. Arquitectura de Capas (Clean Architecture)

```
┌─────────────────────────────────────────────────────────────┐
│                      MeetingRoom.Api                        │
│  Controllers → ExceptionMiddleware → Swagger → StaticFiles  │
├─────────────────────────────────────────────────────────────┤
│                  MeetingRoom.Application                     │
│  UseCases (Rooms, Status, Reservation, Calendar) → DTOs     │
├─────────────────────────────────────────────────────────────┤
│                 MeetingRoom.Infrastructure                   │
│  EF Core (SQLite) → ProviderFactory → SyncService            │
│  GoogleCalendarProvider / Office365Provider / ZohoProvider   │
├─────────────────────────────────────────────────────────────┤
│                     MeetingRoom.Core                         │
│  Entities (Room) → ValueObjects (CalendarId)                │
│  Enums (RoomStatus, CalendarProvider) → Events (MeetingEvent)│
│  Interfaces (IRoomRepository, ICalendarProvider, Factory)    │
└─────────────────────────────────────────────────────────────┘
```

### 2.1 Core / Domain Layer

**Propósito:** Contiene las reglas de negocio puras, sin dependencias externas.

**Principios DDD aplicados:**

| Concepto DDD | Implementación |
|--------------|----------------|
| **Entity** | `Room` — tiene identidad (Guid), mutabilidad controlada |
| **Value Object** | `CalendarId` — inmutable, sin identidad, validación en construcción |
| **Value Object** | `MeetingEvent` — record inmutable, representa un snapshot del calendario |
| **Aggregate Root** | `Room` — punto de entrada único para modificar el agregado |
| **Domain Event** | (Simplificado) `MeetingEvent` como datos, no como evento de dominio pub/sub |
| **Repository Interface** | `IRoomRepository` — abstracción de persistencia |
| **Domain Service Interface** | `ICalendarProvider` — abstracción del servicio de calendario externo |
| **Factory Interface** | `ICalendarProviderFactory` — patrón Factory para resolver el provider según CalendarProvider |

**Soporte multi-proveedor:**

La entidad `Room` incluye un campo `Provider` de tipo `CalendarProvider` enum con 3 valores:

| Provider | API utilizada | Configuración |
|----------|--------------|---------------|
| `Google` | Google Calendar API v3 (service account) | JSON de service account |
| `Office365` | Microsoft Graph API (client credentials) | `{ clientId, tenantId, clientSecret }` |
| `Zoho` | Zoho Calendar API (OAuth2) | `{ clientId, clientSecret, refreshToken }` |

Cada proveedor implementa la interfaz `ICalendarProvider` y se resuelve mediante `ICalendarProviderFactory`.

**Lógica de dominio en la entidad Room:**

```
GetCurrentStatus(now):
    SI hay evento donde now ∈ [start, end) → Occupied
    SI el próximo evento empieza en <15 min → BusySoon
    SI NO → Free
```

**Patrón de diseño:** Las entidades contienen lógica de negocio (no modelos anémicos).

### 2.2 Application Layer

**Propósito:** Orquestación de casos de uso. No contiene lógica de negocio, solo coordina.

**Patrón utilizado:** Casos de uso como clases independientes (estilo CQRS simplificado, sin MediatR para mantener simpleza).

**Estructura de un caso de uso:**

```
public sealed class GetRooms(IRoomRepository repository)
{
    public async Task<IReadOnlyList<RoomDto>> HandleAsync(CancellationToken ct)
    {
        var rooms = await repository.GetAllAsync(ct);
        return rooms.Select(Map).ToList();
    }
}
```

**DTOs:** Records inmutables para transferencia de datos entre capas.

### 2.3 Infrastructure Layer

**Persistencia — Entity Framework Core + SQLite:**

- `AppDbContext` con `DbSet<Room>`
- Configuración vía `IEntityTypeConfiguration<Room>` (Fluent API)
- `CurrentEvents` se serializa como JSON en una columna `TEXT` usando `ValueConverter`
- La BD se crea automáticamente con `EnsureCreated()` al iniciar

**Mapeo Room → BD:**

| Propiedad | Columna SQLite | Tipo | Restricciones |
|-----------|---------------|------|---------------|
| Id | Id | TEXT (GUID) | PK |
| Name | Name | TEXT | NOT NULL, max 200 |
| Capacity | Capacity | INTEGER | NOT NULL |
| CalendarId | CalendarId | TEXT | NOT NULL, max 500 |
| Provider | Provider | TEXT | NOT NULL, max 20 (enum: Google, Office365, Zoho) |
| CurrentEvents | CurrentEvents | TEXT (JSON) | - |

**Integración multi-proveedor de calendarios:**

El sistema usa el patrón **Strategy + Factory** para soportar múltiples proveedores de calendario de forma intercambiable. Cada sala especifica su `Provider` (`Google`, `Office365`, `Zoho`) que determina qué implementación se usa.

**`ICalendarProvider` (interfaz común):**
```
- InitializeAsync(credentialsJson)
- GetEventsAsync(calendarId, from, to) → IReadOnlyList<MeetingEvent>
- CreateQuickEventAsync(calendarId, summary, start, duration) → MeetingEvent
```

**`ICalendarProviderFactory` (patrón Factory):**
```
- GetProvider(CalendarProvider) → ICalendarProvider
- InitializeProviderAsync(CalendarProvider, credentialsJson)
```

**Implementaciones incluidas:**

| Clase | Proveedor | API / SDK |
|-------|-----------|-----------|
| `GoogleCalendarProvider` | Google | Google.Apis.Calendar.v3 (service account JSON) |
| `Office365CalendarProvider` | Office365 | Microsoft Graph REST API (client credentials OAuth2) |
| `ZohoCalendarProvider` | Zoho | Zoho Calendar REST API (OAuth2 refresh token) |

**Formatos de credenciales por proveedor:**

| Proveedor | Formato JSON esperado |
|-----------|----------------------|
| **Google** | `{ "type": "service_account", "project_id": "...", "private_key": "...", "client_email": "..." }` |
| **Office365** | `{ "clientId": "guid", "tenantId": "guid", "clientSecret": "..." }` |
| **Zoho** | `{ "clientId": "...", "clientSecret": "...", "refreshToken": "..." }` |

**CalendarSyncService (BackgroundService):**

- Corre como `IHostedService` en bucle infinito
- Intervalo: 60 segundos
- Itera todas las salas → resuelve el provider vía Factory → consulta calendario → actualiza BD
- Tolerante a fallos: si una sala o proveedor falla, continúa con las demás
- Logging estructurado con `ILogger<T>`

### 2.4 API Layer

**Endpoints REST:**

| Método | Ruta | Controller | Use Case |
|--------|------|-----------|----------|
| GET | /api/rooms | RoomsController | GetRooms |
| GET | /api/rooms/{id} | RoomsController | GetRoomById |
| POST | /api/rooms | RoomsController | CreateRoom |
| PUT | /api/rooms/{id} | RoomsController | UpdateRoom |
| DELETE | /api/rooms/{id} | RoomsController | DeleteRoom |
| GET | /api/status | StatusController | GetAllRoomStatuses |
| GET | /api/status/{id} | StatusController | GetRoomStatus |
| POST | /api/reservations/quick | ReservationController | QuickReserve |
| POST | /api/calendar/credentials | CalendarController | (directo al servicio) |
| POST | /api/calendar/sync | CalendarController | SyncCalendars |

**Middleware de excepciones:**

| Tipo de excepción | HTTP Status | Respuesta |
|-------------------|-------------|-----------|
| `DomainException` | 400 Bad Request | `{ "error": "mensaje" }` |
| `InvalidOperationException` (no init) | 503 Service Unavailable | `{ "error": "Google Calendar not configured..." }` |
| Cualquier otra | 500 Internal Server Error | `{ "error": "Internal server error" }` |

**Servicio de archivos estáticos (Angular):**

- `UseDefaultFiles()` → busca index.html por defecto
- `UseStaticFiles()` → sirve wwwroot/
- `MapFallbackToFile("index.html")` → SPA routing (todas las rutas no-API redirigen a index.html)

**Swagger:**

- Disponible en `/swagger` (modo desarrollo y producción)
- Documento OpenAPI generado automáticamente

### 2.5 Inyección de Dependencias

Registro de servicios por capa siguiendo el patrón de método de extensión:

```
Program.cs
    → builder.Services.AddInfrastructure(connectionString)
        → AddDbContext<AppDbContext>(SQLite)
        → AddScoped<IRoomRepository, RoomRepository>()
        → AddSingleton<ICalendarProviderFactory, CalendarProviderFactory>()
        → AddSingleton<ICalendarSyncService, ManualCalendarSyncService>()
        → AddHostedService<CalendarSyncService>()
    → builder.Services.AddSingleton<GetRooms>()
    → builder.Services.AddSingleton<GetRoomById>()
    → ... (todos los use cases como Singleton)
    → builder.Services.AddControllers()
    → builder.Services.AddSwaggerGen()
```

## 3. Frontend — Angular 19

### 3.1 Estructura

```
src/app/
├── app.component.ts          # Root component (router-outlet)
├── app.config.ts             # provideRouter, provideHttpClient
├── app.routes.ts             # Lazy-loaded routes
├── core/
│   ├── models/               # TypeScript interfaces
│   └── services/             # RoomsService (HTTP + Signals)
├── shared/
│   └── components/           # StatusIndicatorComponent (reusable)
└── features/
    ├── kiosk/                # KioskComponent (vista tablet)
    └── admin/
        ├── admin.component.ts   # Layout con sidebar
        └── pages/
            ├── dashboard-page.component.ts
            ├── rooms-page.component.ts
            └── settings-page.component.ts
```

### 3.2 Patrones y decisiones

| Decisión | Justificación |
|----------|---------------|
| **Standalone Components** | Sin NgModules, imports declarativos por componente |
| **Signals** | Estado reactivo sin RxJS para UI (más simple y performante) |
| **Lazy loading por ruta** | Cada feature se carga bajo demanda |
| **Hash routing** | `withHashLocation()` — compatible con SPA servido desde backend .NET |
| **Tailwind CSS 4** | Utility-first, zero config con `@tailwindcss/postcss` |
| **Sin state management library** | Signals + servicio simple es suficiente para este alcance |

### 3.3 Servicio RoomsService

```typescript
@Injectable({ providedIn: 'root' })
export class RoomsService {
  rooms = signal<Room[]>([]);
  allStatuses = signal<RoomStatus[]>([]);

  // Métodos HTTP con RxJS observables
  // Signals actualizados en respuestas exitosas
}
```

### 3.4 Componente Kiosko

**Ciclo de vida:**
1. `constructor`: obtiene roomId de la ruta, inicia timers
2. `ngOnInit`: carga estado inicial
3. `setInterval(1000)`: actualiza reloj
4. `setInterval(30000)`: polling de estado
5. `ngOnDestroy`: limpia timers

**Estados visuales:**

| Estado | Clase CSS | Efecto |
|--------|-----------|--------|
| Free | `bg-gray-950` | Fondo oscuro neutro |
| BusySoon | `bg-yellow-950` | Fondo amarillo muy oscuro |
| Occupied | `bg-red-950` | Fondo rojo muy oscuro |

### 3.5 Panel de administración

- **Layout:** Sidebar fijo (64px ancho) + contenido scrollable
- **Dashboard:** grid responsive de tarjetas por sala
- **Rooms:** formulario reactivo (crear/editar) + listado con acciones
- **Settings:** textarea para credenciales + botón de sync

## 4. Docker — Multi-stage Build

### 4.1 Etapas

```
Stage 1: node:22-alpine
    → npm install
    → npm run build (Angular → wwwroot/)

Stage 2: dotnet/sdk:10.0-alpine
    → dotnet restore
    → dotnet publish -c Release
    → Copia wwwroot Angular al output

Stage 3: dotnet/aspnet:10.0-alpine
    → Solo runtime (~120 MB final)
    → ASPNETCORE_URLS=http://+:8080
    → Entrypoint: dotnet MeetingRoom.Api.dll
```

### 4.2 Docker Compose

```yaml
services:
  app:
    build: .
    ports: ["8080:8080"]
    volumes: [meetingroom-data:/app/data]
    environment:
      - ConnectionStrings__Default=Data Source=/app/data/meetingrooms.db
```

El volumen `meetingroom-data` persiste la BD SQLite entre reinicios del contenedor.

## 5. Base de Datos — SQLite

**¿Por qué SQLite?**

- Cero configuración: sin servidor externo que administrar
- Portabilidad máxima: un solo archivo `.db`
- Ideal para despliegues single-instance como este
- Suficiente para el volumen de datos (decenas de salas, cientos de eventos)

**Ubicación:** `/app/data/meetingrooms.db` (dentro del contenedor, montada como volumen)

## 6. Consideraciones de Seguridad

| Aspecto | Implementación |
|---------|----------------|
| **Credenciales Google** | Se almacenan en memoria (Singleton). En producción, considerar secrets manager o variables de entorno cifradas |
| **CORS** | Configurado como `AllowAny` para simplificar el despliegue unificado. En producción con frontend separado, restringir a orígenes conocidos |
| **Validación** | Value Objects validan en construcción. DomainException previene estados inválidos |
| **Errores** | Nunca se exponen stack traces al cliente (middleware captura todo) |

## 7. Testing

### 7.1 Tests de dominio (RoomTests)

Validan la lógica de negocio pura, sin dependencias externas:

- Creación de sala con parámetros válidos e inválidos
- Cálculo de estado en los 3 escenarios (Free, BusySoon, Occupied)
- Recuperación de reunión actual y próxima
- Inmutabilidad de la colección de eventos tras `UpdateEvents`

### 7.2 Tests del servicio de calendario (GoogleCalendarServiceTests)

- Verifica que `GetEventsAsync` lanza excepción si no se ha inicializado
- Prueba con mock del servicio (sin conectarse a Google)

### 7.3 Ejecución

```bash
cd src/backend
dotnet test
```

## 8. Diagrama de Componentes

```
                      ┌──────────────┐
                      │   Internet   │
                      └──────┬───────┘
                             │
                    ┌────────┴────────┐
                    │  Google Calendar │
                    │       API        │
                    └────────▲────────┘
                             │
              ┌──────────────┴──────────────┐
              │        Docker Container      │
              │                              │
              │  ┌────────────────────────┐  │
              │  │    MeetingRoom.Api     │  │
              │  │    :8080               │  │
              │  │                        │  │
              │  │  /api/* → Controllers  │  │
              │  │  /*     → Angular SPA  │  │
              │  └───────────┬────────────┘  │
              │              │               │
              │  ┌───────────┴────────────┐  │
              │  │    Infrastructure      │  │
              │  │  CalendarSyncService   │──┼──→ Google API
              │  │  GoogleCalendarService │  │
              │  │  RoomRepository        │  │
              │  └───────────┬────────────┘  │
              │              │               │
              │  ┌───────────┴────────────┐  │
              │  │  meetingrooms.db       │  │
              │  │  (SQLite)              │  │
              │  └────────────────────────┘  │
              └──────────────────────────────┘
                       │
              ┌────────┴────────┐
              │    Tablet       │
              │  (modo kiosko)  │
              │  /kiosk/:roomId │
              └─────────────────┘
                       │
              ┌────────┴────────┐
              │    Admin PC     │
              │  /admin         │
              └─────────────────┘
```

## 9. Guía de Extensión

### Añadir un nuevo campo a Room

1. Añadir propiedad en `Core/Domain/Entities/Room.cs`
2. Añadir campo al DTO en `Application/DTOs/RoomDto.cs`
3. Actualizar mapeo en el caso de uso correspondiente
4. Actualizar `Infrastructure/Persistence/Configurations/RoomConfiguration.cs`
5. Actualizar el formulario en `frontend/features/admin/pages/rooms-page.component.ts`
6. Actualizar el modelo TypeScript en `frontend/core/models/room.model.ts`

### Añadir un nuevo endpoint

1. Si es nueva funcionalidad de dominio → `Core/`
2. Crear caso de uso en `Application/UseCases/`
3. Crear DTO si es necesario en `Application/DTOs/`
4. Implementar lógica de infraestructura si se requiere en `Infrastructure/`
5. Crear controller en `API/Controllers/`
6. Registrar use case en `API/Program.cs`
7. Documentar en este archivo
