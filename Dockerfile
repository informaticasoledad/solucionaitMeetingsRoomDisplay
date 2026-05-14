# Stage 1: Build Angular frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app
COPY src/frontend/package.json src/frontend/package-lock.json* ./
RUN npm install
COPY src/frontend/ .
RUN npm run build

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build
WORKDIR /src
COPY src/backend/*.sln ./
COPY src/backend/src/Core/MeetingRoom.Core/*.csproj src/Core/MeetingRoom.Core/
COPY src/backend/src/Application/MeetingRoom.Application/*.csproj src/Application/MeetingRoom.Application/
COPY src/backend/src/Infrastructure/MeetingRoom.Infrastructure/*.csproj src/Infrastructure/MeetingRoom.Infrastructure/
COPY src/backend/src/API/MeetingRoom.Api/*.csproj src/API/MeetingRoom.Api/
COPY src/backend/tests/MeetingRoom.Tests/*.csproj tests/MeetingRoom.Tests/
RUN dotnet restore
COPY src/backend/ .
COPY --from=frontend-build /app/dist/meeting-room-display/browser /tmp/wwwroot
RUN dotnet publish src/API/MeetingRoom.Api/MeetingRoom.Api.csproj -c Release -o /app /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=backend-build /app .
COPY --from=frontend-build /app/dist/meeting-room-display/browser wwwroot/
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "MeetingRoom.Api.dll"]
