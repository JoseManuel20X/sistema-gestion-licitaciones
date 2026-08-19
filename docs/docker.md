# Docker y Docker Compose

Instrucciones reproducibles para construir y ejecutar la solución en
contenedores (enunciado §13.1).

## Requisito previo

Docker con Compose v2. Nada más: no hace falta instalar .NET ni PostgreSQL en la
máquina, porque ambos viven en las imágenes.

## Arranque en un solo paso

```bash
docker compose up --build
```

Deja disponible la API en `http://localhost:8080` y su documentación interactiva
en `http://localhost:8080/swagger`. Las migraciones y los datos semilla se
aplican solos al arrancar, así que no hay pasos manuales.

Para detenerlo conservando los datos:

```bash
docker compose down
```

Para detenerlo **borrando** los datos:

```bash
docker compose down -v
```

## Servicios

| Servicio | Imagen | Puerto anfitrión | Función |
|---|---|---|---|
| `postgres` | `postgres:16-alpine` | 5433 | Base de datos con volumen persistente |
| `api` | Construida desde `src/Licitaciones.Api/Dockerfile` | 8080 | API REST |

### Por qué PostgreSQL se publica en 5433 y no en 5432

Una instalación nativa de PostgreSQL en la máquina anfitriona ocupa el 5432 y
deja el reenvío de Docker en la sombra. El síntoma es un fallo de autenticación
desconcertante: la aplicación se conecta al servidor equivocado. Publicarlo en
5433 evita el choque en cualquier máquina.

Dentro de la red de Compose los servicios siguen usando el 5432 estándar, porque
ahí no hay conflicto posible.

## Las imágenes

Ambos proyectos, `Licitaciones.Api` y `Licitaciones.Web`, tienen su propio
`Dockerfile` multi-etapa:

1. **Compilación** sobre `sdk:9.0-alpine`. Primero se copian solo los archivos de
   proyecto y se restaura; el código viene después. Mientras las dependencias no
   cambien, Docker reutiliza la capa de restore y la construcción es mucho más
   rápida.
2. **Ejecución** sobre `aspnet:9.0-alpine`, que no lleva el SDK y ocupa bastante
   menos.

### Detalles que costaron un fallo cada uno

**`.editorconfig` viaja a la imagen.** Contiene las supresiones justificadas de
reglas de análisis. Sin él, la compilación dentro del contenedor fallaba con
errores que no ocurrían en local.

**Se compila en Release, y el análisis de estilo es más estricto ahí.** La
primera construcción falló con `IDE0040` aunque `dotnet build` local pasaba. Por
eso la CI también compila en Release: para detectar lo mismo que el despliegue.

**Alpine no trae zonas horarias ni ICU.** Se instalan `tzdata` e `icu-libs`
porque la aplicación presenta fechas en `America/Costa_Rica` y montos con cultura
`es-CR`. Sin ellos, `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` se activaría y los
formatos saldrían mal.

**Usuario sin privilegios.** Ambas imágenes crean el usuario `licitaciones` y
ejecutan como él. Si alguien logra ejecutar código en el contenedor, no lo hace
como root.

## Comprobaciones de salud

`postgres` usa `pg_isready`. La API expone `/salud`, que además de responder
verifica que alcanza la base de datos: un contenedor que responde pero no llega a
PostgreSQL no está listo para recibir tráfico.

La API declara `depends_on: postgres: condition: service_healthy`, así que no
arranca hasta que la base acepte conexiones. Sin eso fallaría al migrar.

## Persistencia

Los datos viven en el volumen con nombre `datos-postgres`, no dentro del
contenedor. Comprobación de que sobreviven a recrear los contenedores:

```bash
docker compose up -d
curl -X POST http://localhost:8080/api/v1/proveedores \
  -H "Content-Type: application/json" -d '{"nombre":"Empresa Central"}'

docker compose down          # elimina los contenedores, conserva el volumen
docker compose up -d

curl http://localhost:8080/api/v1/proveedores   # el proveedor sigue ahí
```

Verificado: el mismo identificador aparece antes y después.

## Credenciales

Las de `compose.yaml` son de desarrollo local y están a la vista a propósito: no
dan acceso a nada fuera de la máquina. Las de un entorno real llegan por
variables de entorno o secretos, nunca por el repositorio (§11).

## Problemas frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| `password authentication failed` | Otro PostgreSQL ocupa el 5432 | Ya se publica en 5433; comprueba a qué puerto apunta tu cadena de conexión |
| La API arranca y muere | PostgreSQL aún no acepta conexiones | El `healthcheck` lo evita; si persiste, revisa `docker compose logs postgres` |
| `Cannot write DateTimeOffset with Offset=-06:00` | Fecha enviada sin convertir a UTC | Corregido: las fechas se convierten antes de persistir |
| Los datos desaparecieron | Se usó `down -v` | `-v` borra el volumen; usa `down` a secas |

## Despliegue en Kubernetes

Ver [kubernetes.md](kubernetes.md).
