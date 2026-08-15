# Imagen multi-etapa para la API (enunciado §13.1).
# El contexto de construcción es la raíz del repositorio, porque el proyecto
# referencia a Domain, Application e Infrastructure.

# ---------- Etapa de compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /origen

# Primero solo los archivos de proyecto: mientras las dependencias no cambien,
# Docker reutiliza la capa de restore y la construcción es mucho más rápida.
# .editorconfig se copia junto a Directory.Build.props: contiene las supresiones
# justificadas de reglas de análisis, y sin él la compilación dentro del
# contenedor falla con errores que no ocurren en local.
COPY Directory.Build.props global.json .editorconfig ./
COPY tests/Directory.Build.props tests/
COPY src/Licitaciones.Domain/*.csproj src/Licitaciones.Domain/
COPY src/Licitaciones.Application/*.csproj src/Licitaciones.Application/
COPY src/Licitaciones.Infrastructure/*.csproj src/Licitaciones.Infrastructure/
COPY src/Licitaciones.Api/*.csproj src/Licitaciones.Api/

RUN dotnet restore src/Licitaciones.Api/Licitaciones.Api.csproj

COPY src/ src/

RUN dotnet publish src/Licitaciones.Api/Licitaciones.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /publicacion

# ---------- Etapa de ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /aplicacion

# Los datos de zonas horarias no vienen en la imagen Alpine y la aplicación
# presenta las fechas en America/Costa_Rica (§8.2).
RUN apk add --no-cache tzdata icu-libs

# Usuario sin privilegios: si alguien consigue ejecutar código en el contenedor,
# no lo hace como root (§13.1).
RUN addgroup -S licitaciones && adduser -S licitaciones -G licitaciones
USER licitaciones

COPY --from=build --chown=licitaciones:licitaciones /publicacion .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "Licitaciones.Api.dll"]
