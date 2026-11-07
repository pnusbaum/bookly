# syntax=docker/dockerfile:1

# ---------- 1. Frontend: build React (Vite) ----------
FROM node:24-slim AS web-build
WORKDIR /src
COPY src/Bookly.Web/package.json src/Bookly.Web/package-lock.json ./
RUN npm ci
COPY src/Bookly.Web/ ./
RUN npm run build

# ---------- 2. Backend: publish .NET ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY global.json ./
COPY src/Bookly.Api/Bookly.Api.csproj src/Bookly.Api/
RUN dotnet restore src/Bookly.Api/Bookly.Api.csproj
COPY src/Bookly.Api/ src/Bookly.Api/
RUN dotnet publish src/Bookly.Api/Bookly.Api.csproj -c Release -o /app --no-restore

# ---------- 3. Runtime: tylko to, co potrzebne do działania ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=api-build /app ./
COPY --from=web-build /src/dist ./wwwroot
# Cloud Run domyślnie wysyła ruch na port 8080
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Bookly.Api.dll"]