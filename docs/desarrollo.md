# Guía de Desarrollo — Meeting Room Display

## 1. Configuración del Entorno de Desarrollo

### 1.1 Herramientas necesarias

| Herramienta | Versión mínima | Instalación |
|-------------|---------------|-------------|
| .NET SDK | 10.0 | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Node.js | 22 | [nodejs.org](https://nodejs.org/) |
| npm | 10 | (incluido con Node.js) |
| Angular CLI | 19 | `npm install -g @angular/cli@19` |
| Git | 2.40 | [git-scm.com](https://git-scm.com/) |
| Docker | 24 | [docker.com](https://www.docker.com/products/docker-desktop/) |
| IDE recomendado | - | Rider 2025 o VS Code + C# Dev Kit |

### 1.2 Clonar y preparar

```bash
git clone <repo-url>
cd meeting-room-display

# Backend
cd src/backend
dotnet restore

# Frontend
cd ../frontend
npm install
```

## 2. Ejecución en Desarrollo

### 2.1 Backend (.NET API)

```bash
cd src/backend
dotnet run --project src/API/MeetingRoom.Api

# API en:              http://localhost:5000
# Swagger en:          http://localhost:5000/swagger
```

La BD SQLite se crea automáticamente en el directorio del proyecto.

### 2.2 Frontend (Angular)

```bash
cd src/frontend
npm start

# Dev server en:       http://localhost:4200
# Proxy configurado a: http://localhost:5000 (API)
```

El proxy (`proxy.conf.json`) redirige peticiones `/api/*` al backend automáticamente.

### 2.3 Ambos simultáneamente

```bash
# Terminal 1 — Backend
cd src/backend && dotnet run --project src/API/MeetingRoom.Api

# Terminal 2 — Frontend
cd src/frontend && npm start
```

## 3. Ejecución de Tests

```bash
cd src/backend

# Todos los tests
dotnet test

# Tests específicos
dotnet test --filter "FullyQualifiedName~Domain"

# Con output detallado
dotnet test -v detailed

# Con coverage (requiere paquete adicional)
dotnet test /p:CollectCoverage=true
```

## 4. Estructura de Archivos

```
solucionaitMeetingRoomDisplay/
├── README.md
├── Dockerfile
├── docker-compose.yml
├── docs/
│   ├── funcional.md          ← Especificación funcional
│   ├── tecnica.md            ← Arquitectura y diseño técnico
│   ├── despliegue.md         ← Guía de despliegue y operaciones
│   └── desarrollo.md         ← Este archivo
│
├── src/
│   ├── backend/
│   │   ├── MeetingRoom.sln
│   │   ├── src/
│   │   │   ├── Core/
│   │   │   │   └── MeetingRoom.Core/
│   │   │   │       ├── Domain/
│   │   │   │       │   ├── Entities/Room.cs
│   │   │   │       │   ├── Enums/RoomStatus.cs
│   │   │   │       │   ├── ValueObjects/CalendarId.cs
│   │   │   │       │   └── Events/MeetingEvent.cs
│   │   │   │       └── Interfaces/
│   │   │   │           ├── IRoomRepository.cs
│   │   │   │           └── IGoogleCalendarService.cs
│   │   │   ├── Application/
│   │   │   │   └── MeetingRoom.Application/
│   │   │   │       ├── DTOs/
│   │   │   │       │   ├── RoomDto.cs
│   │   │   │       │   ├── RoomStatusDto.cs
│   │   │   │       │   └── GoogleCredentialsDto.cs
│   │   │   │       ├── UseCases/
│   │   │   │       │   ├── Rooms/RoomUseCases.cs
│   │   │   │       │   ├── Status/StatusUseCases.cs
│   │   │   │       │   ├── Reservation/QuickReserve.cs
│   │   │   │       │   └── Calendar/SyncCalendars.cs
│   │   │   │       └── Interfaces/ICalendarSyncService.cs
│   │   │   ├── Infrastructure/
│   │   │   │   └── MeetingRoom.Infrastructure/
│   │   │   │       ├── Persistence/
│   │   │   │       │   ├── AppDbContext.cs
│   │   │   │       │   └── Configurations/RoomConfiguration.cs
│   │   │   │       ├── Repositories/RoomRepository.cs
│   │   │   │       ├── Services/
│   │   │   │       │   ├── GoogleCalendarService.cs
│   │   │   │       │   └── CalendarSyncService.cs
│   │   │   │       └── DependencyInjection.cs
│   │   │   └── API/
│   │   │       └── MeetingRoom.Api/
│   │   │           ├── Controllers/
│   │   │           │   ├── RoomsController.cs
│   │   │           │   ├── StatusController.cs
│   │   │           │   ├── ReservationController.cs
│   │   │           │   └── CalendarController.cs
│   │   │           ├── Middleware/ExceptionMiddleware.cs
│   │   │           ├── Program.cs
│   │   │           ├── appsettings.json
│   │   │           └── wwwroot/         ← Angular build output
│   │   └── tests/
│   │       └── MeetingRoom.Tests/
│   │           ├── Domain/RoomTests.cs
│   │           └── Services/GoogleCalendarServiceTests.cs
│   │
│   └── frontend/
│       ├── angular.json
│       ├── package.json
│       ├── proxy.conf.json
│       ├── tsconfig.json
│       ├── postcss.config.mjs
│       └── src/
│           ├── index.html
│           ├── styles.css              ← @import "tailwindcss"
│           ├── main.ts
│           └── app/
│               ├── app.component.ts
│               ├── app.config.ts
│               ├── app.routes.ts
│               ├── core/
│               │   ├── models/
│               │   │   ├── room.model.ts
│               │   │   └── room-status.model.ts
│               │   └── services/
│               │       └── rooms.service.ts
│               ├── shared/
│               │   └── components/
│               │       └── status-indicator.component.ts
│               └── features/
│                   ├── kiosk/
│                   │   └── kiosk.component.ts
│                   └── admin/
│                       ├── admin.component.ts
│                       └── pages/
│                           ├── dashboard-page.component.ts
│                           ├── rooms-page.component.ts
│                           └── settings-page.component.ts
```

## 5. Convenciones de Código

### 5.1 C# Backend

| Convención | Regla |
|------------|-------|
| **Namespaces** | File-scoped (`namespace X;`) |
| **Clases** | `sealed` por defecto, a menos que se diseñe para herencia |
| **Registros** | `sealed record` para DTOs y value objects |
| **Constructores primarios** | `class Foo(IBar bar) { }` — usar cuando tenga sentido |
| **Nullable** | `#nullable enable` en todo el proyecto |
| **Async** | Sufijo `Async` en métodos que devuelven Task |
| **CancellationToken** | Siempre como último parámetro con default |
| **Inyección** | Solo por constructor (no `[FromServices]` en actions) |
| **Excepciones de dominio** | Usar `DomainException`, nunca lanzar excepciones genéricas para reglas de negocio |

### 5.2 TypeScript / Angular

| Convención | Regla |
|------------|-------|
| **Componentes** | Standalone, sin NgModules |
| **Estado** | Usar Signals para estado local, RxJS solo para HTTP |
| **Modelos** | Interfaces, no clases |
| **Inyección** | `inject()` en lugar de constructor injection |
| **Estilos** | Tailwind utility classes, sin archivos CSS por componente |
| **Imports** | Rutas relativas (no path aliases como `@app/`) |
| **Formularios** | Reactive Forms (`FormBuilder`) |

### 5.3 Commits

```
feat: add quick reservation feature
fix: correct busy-soon status calculation
docs: add deployment guide
refactor: extract calendar sync to background service
test: add room domain tests
```

## 6. Workflow de Desarrollo

### Añadir una feature nueva

```
1. Crear/actualizar entidad de dominio en Core
2. Crear interfaz en Core (si es nuevo servicio/repo)
3. Crear DTOs en Application
4. Crear caso de uso en Application/UseCases
5. Implementar en Infrastructure
6. Registrar en Infrastructure/DependencyInjection.cs
7. Crear controller en API
8. Registrar use case en Program.cs
9. Actualizar frontend (modelos, servicio, componentes)
10. Escribir tests
11. Actualizar Swagger (automático)
12. Actualizar documentación en docs/
```

### Debugging

**Backend:**
- `dotnet run` en modo Debug con breakpoints en el IDE
- Swagger en `/swagger` para probar endpoints manualmente
- Logs con `ILogger<T>` en los servicios

**Frontend:**
- `npm start` con Chrome DevTools
- Angular DevTools para inspeccionar Signals y componentes
- Network tab para ver peticiones HTTP al backend

## 7. Dependencias Clave y Versiones

### Backend (NuGet)

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.0-preview.* | ORM SQLite |
| `Google.Apis.Calendar.v3` | 1.69.0 | Google Calendar API |
| `Google.Apis.Auth` | 1.69.0 | Autenticación Google |
| `Swashbuckle.AspNetCore` | 7.3.1 | Swagger/OpenAPI |
| `xunit` | 2.9.3 | Testing framework |
| `Moq` | 4.20.72 | Mocking |

### Frontend (npm)

| Paquete | Versión | Uso |
|---------|---------|-----|
| `@angular/core` | ^19.2.0 | Framework |
| `@angular/router` | ^19.2.0 | Routing con lazy loading |
| `tailwindcss` | ^4.0.0 | Utility-first CSS |
| `@tailwindcss/postcss` | ^4.0.0 | PostCSS plugin Tailwind 4 |

## 8. Integración Continua (ejemplo GitHub Actions)

```yaml
name: CI
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test src/backend/MeetingRoom.sln
```
