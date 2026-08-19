# Imagen multi-etapa para la interfaz web (enunciado §13.1).
# El contexto de construcción es la raíz del repositorio, porque el proyecto
# referencia a Domain, Application e Infrastructure.

# ---------- Etapa de compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /origen

# Primero solo los archivos de proyecto: mientras las dependencias no cambien,
# Docker reutiliza la capa de restore y la construcción es mucho más rápida.
# .editorconfig viaja con ellos porque contiene las supresiones justificadas de
# análisis, sin las cuales la compilación en Release falla dentro del contenedor.
COPY Directory.Build.props global.json .editorconfig ./
COPY tests/Directory.Build.props tests/
COPY src/Licitaciones.Domain/*.csproj src/Licitaciones.Domain/
COPY src/Licitaciones.Application/*.csproj src/Licitaciones.Application/
COPY src/Licitaciones.Infrastructure/*.csproj src/Licitaciones.Infrastructure/
COPY src/Licitaciones.Web/*.csproj src/Licitaciones.Web/

RUN dotnet restore src/Licitaciones.Web/Licitaciones.Web.csproj

COPY src/ src/

RUN dotnet publish src/Licitaciones.Web/Licitaciones.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /publicacion

# ---------- Etapa de ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /aplicacion

# Alpine no trae datos de zonas horarias ni ICU, y la interfaz presenta fechas en
# America/Costa_Rica y montos con cultura es-CR (§8.2 y §9).
RUN apk add --no-cache tzdata icu-libs

# Usuario sin privilegios: si alguien logra ejecutar código en el contenedor, no
# lo hace como root (§13.1).
RUN addgroup -S licitaciones && adduser -S licitaciones -G licitaciones
USER licitaciones

COPY --from=build --chown=licitaciones:licitaciones /publicacion .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "Licitaciones.Web.dll"]
