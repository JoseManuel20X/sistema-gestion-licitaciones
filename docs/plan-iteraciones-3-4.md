# Plan de trabajo — iteraciones 3 y 4

Detalle técnico de las iteraciones pendientes: qué está terminado, qué falta, en
qué orden abordarlo y qué contratos ya existen para no reinventarlos. Complementa
el [plan XP](plan-xp.md), que fija el plan de liberación y las reglas de trabajo.

---

## 1. Qué está terminado

| Capa | Estado |
|---|---|
| `Licitaciones.Domain` | ✅ Completo. Todas las reglas del §8. |
| `Licitaciones.Application` | ✅ Casos de uso de proveedores, licitaciones, ofertas y niveles. |
| `Licitaciones.Infrastructure` | ✅ EF Core 9, PostgreSQL, migración inicial, semilla, repositorios. |
| `Licitaciones.UnitTests` | ✅ 183 pruebas. |
| `Licitaciones.IntegrationTests` | ✅ 13 pruebas con PostgreSQL real. |
| `Licitaciones.Web` | ⛔ Plantilla vacía. |
| `Licitaciones.Api` | ⛔ Plantilla vacía. |
| Docker · Kubernetes · CI | ⛔ No existen. |

**Regla de oro: no dupliques reglas de negocio.** Todo lo del §8 ya está
implementado y probado. La segunda mitad es presentación y despliegue. Si te ves
escribiendo un `if` sobre montos o fechas en un controlador, está mal: llama al
caso de uso.

## 2. Puesta en marcha

```bash
git clone <url-del-repositorio>
cd Proyecto
dotnet restore
dotnet tool restore
```

Levantar PostgreSQL local (hasta que exista `docker-compose.yml`):

```bash
docker run -d --name licitaciones-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=licitaciones -p 5432:5432 postgres:16-alpine
```

Verificar que todo está verde antes de tocar nada:

```bash
dotnet build && dotnet test
```

Las pruebas de integración necesitan Docker en ejecución.

## 3. Los contratos que vas a usar

### Casos de uso disponibles

Todos están registrados con `servicios.AgregarAplicacion()` y se inyectan por
constructor.

| Servicio | Métodos |
|---|---|
| `ProveedorServicio` | `CrearAsync`, `ActualizarAsync`, `ObtenerAsync`, `ListarAsync`, `EliminarAsync` |
| `LicitacionServicio` | `CrearAsync`, `ActualizarAsync`, `ObtenerAsync`, `ListarAsync`, `CambiarEstadoAsync`, `ObtenerMejorOfertaAsync`, `EliminarAsync` |
| `OfertaServicio` | `RegistrarAsync`, `ActualizarAsync`, `ObtenerAsync`, `ListarAsync`, `EliminarAsync` |
| `NivelAprobacionServicio` | `CrearAsync`, `ActualizarAsync`, `ObtenerAsync`, `ListarAsync`, `ResolverAprobadorAsync`, `EliminarAsync` |

### Cómo se traduce un `Resultado<T>` a HTTP

Esta correspondencia ya está decidida y probada. **Impleméntala una sola vez** en
un método auxiliar del controlador base:

| `TipoError` | HTTP |
|---|---|
| `Validacion` | 400 Bad Request |
| `NoEncontrado` | 404 Not Found |
| `Conflicto` | 409 Conflict |
| `Concurrencia` | 409 Conflict |
| `ReglaNegocio` | 422 Unprocessable Entity |

Esqueleto sugerido:

```csharp
protected IActionResult DesdeResultado<T>(Resultado<T> resultado)
{
    if (resultado.EsExitoso)
    {
        return Ok(resultado.Valor);
    }

    var error = resultado.Error!;
    var estado = error.Tipo switch
    {
        TipoError.Validacion => StatusCodes.Status400BadRequest,
        TipoError.NoEncontrado => StatusCodes.Status404NotFound,
        TipoError.Conflicto or TipoError.Concurrencia => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity,
    };

    var problema = new ProblemDetails
    {
        Title = "No se pudo completar la operación",
        Status = estado,
        Detail = error.Mensaje,
    };
    problema.Extensions["codigoError"] = error.Codigo;
    problema.Extensions["idCorrelacion"] = HttpContext.TraceIdentifier;

    return StatusCode(estado, problema);
}
```

El §10.2 pide título, estado, detalle seguro, **código de error** e
**identificador de correlación**: los dos últimos van en `Extensions`.

> El mensaje de `ErrorAplicacion` ya está redactado para mostrarse al usuario: no
> contiene rutas internas, consultas ni nombres de restricción. Puedes exponerlo
> tal cual.

### Paginación

`ParametrosConsulta { Pagina, TamanoPagina, Filtro, OrdenarPor, Descendente }`
llega por *query string*. `PaginaResultado<T>` ya trae `TotalElementos`,
`TotalPaginas`, `TienePaginaAnterior` y `TienePaginaSiguiente`.

## 4. Iteración 3 — Moneda, interfaz y API

### HU-09 · Administrar tipos de cambio (2 pts)

La entidad `TipoCambio`, su tabla, el índice único parcial
(`WHERE Activo = true`) y el dato semilla **ya existen**. Falta el caso de uso.

Crea `Licitaciones.Application/TiposCambio/TipoCambioServicio.cs` siguiendo el
patrón de `NivelAprobacionServicio`. El único punto delicado es **activar**:

```csharp
// Desactivar el anterior y activar el nuevo debe ocurrir en la misma
// transacción: el índice único parcial rechaza dos filas con Activo = true.
await _unidadDeTrabajo.EnTransaccionAsync(async ct =>
{
    var activo = await _repositorio.ObtenerActivoAsync(ct);
    activo?.Desactivar(_reloj);
    nuevo.Activar(_reloj);
    await _unidadDeTrabajo.GuardarCambiosAsync(ct);
    return true;
}, cancelacion);
```

`ITipoCambioRepositorio` y `TipoCambioRepositorio` ya están implementados y
registrados.

**TDD:** escribe primero la prueba «activar uno desactiva el anterior» y una de
integración que verifique que nunca hay dos activos.

### HU-10 · Alternar montos CRC/USD (1 pt)

`TipoCambio.ConvertirCrcAUsd` ya existe y está probado. Falta:

- Un servicio de presentación que obtenga el tipo activo y convierta para la vista.
- Un botón que alterne toda la vista entre CRC y USD.
- **Mostrar la fecha del tipo de cambio utilizado** (§8.8), que se olvida con facilidad.
- Formato `es-CR` para colones: usa `Normalizador.CulturaCostaRica`, ya disponible.

Los valores persistidos **nunca** cambian: la conversión es solo de presentación.

### HU-11 y HU-12 · Interfaz web (3 pts)

Empieza por **borrar el código de plantilla** que queda en `Licitaciones.Web`:
`Views/Home/Privacy.cshtml`, la acción `Privacy` de `HomeController` y
`Models/ErrorViewModel.cs` si no lo usas. El §6.4 prohíbe el código muerto.

Configura `Program.cs`:

```csharp
builder.Services.AgregarInfraestructura(
    builder.Configuration.GetConnectionString("Licitaciones")
    ?? throw new InvalidOperationException("Falta la cadena de conexión."));
builder.Services.AgregarAplicacion();
```

Pendientes que la rúbrica revisa explícitamente:

- Landing page que explique el flujo completo: licitación → ofertas → mejor oferta
  → nivel de aprobación → conversión monetaria.
- Menú con Inicio, Licitaciones, Proveedores, Ofertas, Niveles de aprobación,
  Tipo de cambio y documentación de la API.
- Modo claro/oscuro con control visible y persistencia en `localStorage`.
- Selector de **fecha y hora con calendario**, no texto libre (§8.2).
- Validación junto al campo; tablas con paginación, filtro y ordenamiento.
- Confirmación antes de cualquier eliminación (§8.9).
- Recursos front-end **locales**. Bootstrap y jQuery ya están en `wwwroot/lib`;
  no los sustituyas por una CDN: el §9 exige que la interfaz siga siendo usable
  sin acceso a Internet.

### HU-13 · API REST v1 (3 pts)

Endpoints mínimos del §10.1, todos bajo `/api/v1`:

```
GET    /api/v1/licitaciones
GET    /api/v1/licitaciones/{id}
POST   /api/v1/licitaciones
PUT    /api/v1/licitaciones/{id}
PATCH  /api/v1/licitaciones/{id}/estado
DELETE /api/v1/licitaciones/{id}
GET    /api/v1/licitaciones/{id}/ofertas
POST   /api/v1/licitaciones/{id}/ofertas
GET    /api/v1/licitaciones/{id}/mejor-oferta

GET/POST/PUT/DELETE  /api/v1/proveedores
GET/POST/PUT/DELETE  /api/v1/ofertas
GET/POST/PUT/DELETE  /api/v1/niveles-aprobacion
GET/POST/PUT/DELETE  /api/v1/tipos-cambio
PATCH                /api/v1/tipos-cambio/{id}/activar
```

Puntos a cuidar:

- **Nunca expongas entidades de EF Core.** Usa los DTO de `Application`; ya existen.
- `POST` devuelve **201 Created** con cabecera `Location`; `DELETE` devuelve **204**.
- Registra un manejador global de excepciones que devuelva 500 controlado, sin
  trazas de pila (§10.2).
- Documenta con OpenAPI/Swagger y enlázalo desde el menú.
- Crea `docs/api.md` con endpoints, ejemplos y errores, y una colección
  reproducible de solicitudes (`.http` o Postman) dentro de `/docs`.

Al terminar la iteración, escribe `docs/modulos/tipo-cambio.md`,
`docs/modulos/interfaz-web.md` y `docs/modulos/api-rest.md`, y activa sus enlaces
en `docs/README.md`.

## 5. Iteración 4 — Despliegue, CI y E2E

### HU-14 · Docker Compose (2 pts)

`Dockerfile` multi-stage con usuario no privilegiado:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Licitaciones.Web -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
ENTRYPOINT ["dotnet", "Licitaciones.Web.dll"]
```

`compose.yaml` con servicio de aplicación y PostgreSQL, **volumen persistente**,
variables de entorno y *health checks* en ambos. La aplicación debe esperar a que
la base esté sana (`depends_on` con `condition: service_healthy`).

`MigrarYSembrarAsync` ya existe: invócalo al arrancar para que
`docker compose up --build` no requiera pasos manuales.

Verifica la persistencia: crea datos, `docker compose restart`, comprueba que
siguen ahí. Documenta el procedimiento en `docs/docker.md`.

### HU-15 · Kubernetes (3 pts)

Manifiestos en `/k8s` con los nombres exactos que pide el §13.2:
`namespace.yaml`, `app-deployment.yaml`, `app-service.yaml`, `app-configmap.yaml`,
`app-secret.example.yaml`, `postgres-statefulset.yaml`, `postgres-service.yaml`,
`postgres-pvc.yaml`.

- Sondas `startupProbe`, `readinessProbe` y `livenessProbe`. Añade endpoints
  `/health/ready` y `/health/live` en la aplicación.
- `requests` y `limits` de recursos.
- **Migraciones desde un `Job` o `initContainer`**, no desde varias réplicas a la
  vez.
- `app-secret.example.yaml` con valores de ejemplo; el real está en `.gitignore`.

Evidencia en `docs/kubernetes.md`: salidas de `kubectl get pods,svc,pvc`, logs y
comprobación de que los datos sobreviven al reinicio.

### HU-16 · GitHub Actions (2 pts)

`.github/workflows/ci.yml` debe: restaurar, compilar, ejecutar pruebas con
cobertura, verificar formato (`dotnet format --verify-no-changes`), construir la
imagen Docker, validar los manifiestos de Kubernetes y revisar dependencias
vulnerables (`dotnet list package --vulnerable --include-transitive`).

El *runner* de GitHub Actions trae Docker, así que las pruebas de Testcontainers
funcionan sin configuración extra.

Configura la rama `main` como protegida para que un fallo bloquee la integración.

### HU-17 · Pruebas E2E con Playwright (3 pts)

```bash
cd tests/Licitaciones.FunctionalTests
dotnet add package Microsoft.Playwright.NUnit
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install
```

Escenarios exigidos por §12.3: landing y navegación, CRUD de proveedor, creación
/ publicación / cierre de licitación, registro y rechazo de ofertas, modo
claro/oscuro, conversión CRC/USD y mensajes de validación.

## 6. Orden recomendado

No sigas el orden de las historias literalmente. Este orden reduce el retrabajo:

1. **CI primero** (HU-16, versión mínima: restaurar + compilar + probar). Así cada
   commit posterior llega verificado en lugar de acumular problemas para el final.
2. **Docker Compose** (HU-14). Tener PostgreSQL reproducible facilita todo lo demás.
3. **API REST** (HU-13) + tipo de cambio (HU-09). Se prueba con Swagger sin
   depender de la interfaz.
4. **Interfaz web** (HU-10, HU-11, HU-12), ya sobre casos de uso verificados.
5. **Kubernetes** (HU-15).
6. **E2E** (HU-17) al final, cuando la interfaz esté estable; si no, las pruebas
   se romperán en cada cambio.
7. **CI completa**: añade cobertura, formato, imagen Docker y validación de
   manifiestos.

## 7. Lo que la rúbrica castiga y es fácil olvidar

- [ ] Etiquetar la entrega final como `v1.0.0` o `entrega-final` (§14.2).
- [ ] Etiquetar cada iteración (`v0.1.0` … `v0.4.0`) según el plan de liberación.
- [ ] Commits **distribuidos en el tiempo**: el §14 evalúa la distribución, no la
      cantidad. Un volcado masivo el último día es exactamente lo que penaliza.
- [ ] Actualizar `docs/bitacora-xp.md` **durante** cada iteración, no al final.
- [ ] Registrar la asistencia de IA en `docs/uso-ia.md` por iteración y módulo (§16).
- [ ] Ningún secreto en el repositorio: revisa antes de cada push.
- [ ] No subir `bin/`, `obj/` ni carpetas generadas (ya están en `.gitignore`).
- [ ] Terminología **exclusivamente XP**: nunca «sprint», «backlog»,
      «daily» ni «retrospectiva». El §4.2 lo prohíbe y es un descuido caro.
- [ ] Cada historia terminada vinculada con sus commits y pruebas en la bitácora.

## 8. Convenciones que debes mantener

**Commits** — Conventional Commits con ámbito por módulo, reflejando el ciclo TDD:

```
test(tipo-cambio): cubrir activacion excluyente
feat(tipo-cambio): activar desactivando el anterior en una transaccion
refactor(api): extraer traduccion de resultado a controlador base
docs(xp): registrar resultados de la iteracion 3
```

**Código** — español para el dominio, inglés técnico para la infraestructura.
Documentación XML en los miembros públicos relevantes. Comentarios que expliquen
**por qué**, no qué hace la línea siguiente.

**Análisis estático** — `TreatWarningsAsErrors` está activo. Si una regla te
estorba, no la desactives en bloque: suprímela con justificación escrita, como
están las actuales en `.editorconfig`.

**Pruebas** — `Metodo_Escenario_ResultadoEsperado`. Prueba que falla antes de la
implementación, siempre.

## 9. Dónde mirar antes de preguntar

| Duda | Documento |
|---|---|
| Por qué el código está organizado así | [arquitectura-general.md](arquitectura-general.md) |
| Qué garantiza cada índice o restricción | [modelo-datos.md](modelo-datos.md) |
| Qué hace un módulo y con qué errores responde | `modulos/<módulo>.md` |
| Cómo se prueba y por qué | [pruebas.md](pruebas.md) |
| Qué se decidió y por qué en cada iteración | [bitacora-xp.md](bitacora-xp.md) |
| Qué debe poder defenderse oralmente | [uso-ia.md](uso-ia.md) |
