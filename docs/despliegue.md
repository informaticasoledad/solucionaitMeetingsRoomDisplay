# Guía de Despliegue — Meeting Room Display

## 1. Requisitos del Servidor

| Requisito | Mínimo | Recomendado |
|-----------|--------|-------------|
| CPU | 1 vCPU | 2 vCPU |
| RAM | 512 MB | 1 GB |
| Disco | 1 GB | 5 GB (para logs y crecimiento de BD) |
| Docker | 24.0+ | 27.0+ |
| Docker Compose | 2.20+ | 2.30+ |
| Acceso a internet | Sí | Sí (para Google Calendar API) |

## 2. Instalación

### 2.1 Clonar el repositorio

```bash
git clone <repo-url> meeting-room-display
cd meeting-room-display
```

### 2.2 Construir y desplegar

```bash
docker compose up --build -d
```

### 2.3 Verificar que está corriendo

```bash
docker compose ps
# Debe mostrar el servicio "app" con estado "Up"

docker compose logs -f
# Ver los logs en tiempo real
```

### 2.4 Acceder

- **Panel de administración:** `http://<IP_DEL_SERVIDOR>:8080/#/admin`
- **Swagger API:** `http://<IP_DEL_SERVIDOR>:8080/swagger`
- **Vista kiosko:** `http://<IP_DEL_SERVIDOR>:8080/#/kiosk/<ROOM_ID>`

## 3. Configuración Inicial

### Paso 1: Configurar credenciales de calendario

El sistema soporta 3 proveedores. Configura los que necesites:

1. Accede al panel de administración → **Configuración**
2. Selecciona el proveedor en el dropdown (Google / Office365 / Zoho)
3. Pega las credenciales en el formato esperado por ese proveedor
4. Haz clic en **Guardar Credenciales**
5. Repite para cada proveedor adicional que necesites configurar

### Paso 2: Crear salas

1. Ve a **Salas** en el panel de administración
2. Para cada sala física:
   - **Nombre:** ej. "Sala Atlántico"
   - **Capacidad:** ej. 8
   - **Proveedor:** Google / Office365 / Zoho (según qué calendario uses para esa sala)
   - **Calendar ID:** el identificador del calendario de la sala (depende del proveedor)

### Paso 3: Configurar tablets en modo kiosko

Para cada tablet en la puerta de una sala:

**Opción A — Android (Fully Kiosk Browser):**

1. Instala [Fully Kiosk Browser](https://www.fully-kiosk.com/) desde Google Play
2. Configura la URL de inicio: `http://<IP_SERVIDOR>:8080/#/kiosk/<ROOM_ID>`
3. En Fully Kiosk Settings:
   - Start URL: la URL anterior
   - Enable Kiosk Mode: ON
   - Show Status Bar: OFF
   - Screensaver: OFF (o configurar para que nunca se apague)
   - Motion Detection: ON (opcional, para despertar pantalla al acercarse)

**Opción B — Raspberry Pi / Linux:**

```bash
# Instalar Chromium en modo kiosko
sudo apt install chromium-browser

# Crear script de autoarranque
cat > ~/.config/autostart/kiosk.desktop << EOF
[Desktop Entry]
Type=Application
Name=MeetingRoom Kiosk
Exec=chromium-browser --kiosk --disable-infobars --noerrdialogs --incognito http://<IP_SERVIDOR>:8080/#/kiosk/<ROOM_ID>
X-GNOME-Autostart-enabled=true
EOF
```

**Opción C — Windows:**

```powershell
# Crear acceso directo a Chrome en modo kiosko
"C:\Program Files\Google\Chrome\Application\chrome.exe" --kiosk --disable-infobars http://<IP_SERVIDOR>:8080/#/kiosk/<ROOM_ID>
```

## 4. Variables de Entorno

| Variable | Default | Descripción |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://+:8080` | Puerto de escucha |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Entorno (Development/Production) |
| `ConnectionStrings__Default` | `Data Source=/app/data/meetingrooms.db` | Cadena de conexión SQLite |

Para cambiar el puerto:

```yaml
# docker-compose.yml
environment:
  - ASPNETCORE_URLS=http://+:3000
ports:
  - "3000:3000"
```

## 5. Mantenimiento

### Actualizar la aplicación

```bash
git pull
docker compose up --build -d
```

### Ver logs

```bash
# Últimos logs
docker compose logs --tail=100

# Seguir logs en tiempo real
docker compose logs -f

# Solo logs de errores del sync
docker compose logs app | grep -i error
```

### Backup de la base de datos

```bash
# La BD está en el volumen meetingroom-data
docker compose cp app:/app/data/meetingrooms.db ./backup_$(date +%Y%m%d).db
```

### Restaurar base de datos

```bash
docker compose cp ./backup_20260513.db app:/app/data/meetingrooms.db
docker compose restart app
```

### Reiniciar el servicio

```bash
docker compose restart app
```

### Detener el servicio

```bash
docker compose down
```

### Eliminar todo (incluyendo datos)

```bash
docker compose down -v
```

## 6. Solución de Problemas

### La tablet muestra "Cargando…" indefinidamente

1. Verifica que la API está corriendo: `curl http://<IP>:8080/api/status`
2. Verifica que el roomId en la URL corresponde a una sala existente
3. Revisa los logs: `docker compose logs app --tail=50`

### El estado de la sala no se actualiza

1. Verifica que las credenciales de Google están configuradas
2. Ve a Configuración → Sincronizar Ahora
3. Revisa los logs en busca de errores de Google API
4. Verifica que la cuenta de servicio tiene acceso al calendario de la sala

### Error "Google Calendar not configured" al intentar reservar

1. Ve a Configuración en el panel admin
2. Vuelve a pegar y guardar el JSON de credenciales
3. Verifica que el JSON no esté corrupto o incompleto

### La imagen Docker es muy grande

La imagen final debería ser ~120 MB. Si es mayor:
- Verifica que el multi-stage build se ejecutó correctamente
- Asegúrate de que `dotnet publish` usa `-c Release`
- No incluyas `node_modules` en la imagen final

## 7. Producción — Consideraciones Adicionales

### HTTPS

Para exponer con HTTPS, usa un proxy reverso como Nginx o Caddy:

```yaml
# Ejemplo con Caddy añadido al docker-compose
services:
  caddy:
    image: caddy:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
    depends_on:
      - app

  app:
    # ... configuración existente sin exponer puerto externo
```

```caddyfile
# Caddyfile
meetingrooms.miempresa.com {
    reverse_proxy app:8080
}
```

### Monitoreo

```bash
# Health check manual
curl -s http://localhost:8080/api/status | jq '. | length'
# Debe devolver el número de salas configuradas
```

### Rotación de logs

Docker gestiona los logs automáticamente. Para configurar rotación:

```yaml
# docker-compose.yml
services:
  app:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```
