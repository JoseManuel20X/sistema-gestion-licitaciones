# Visión y alcance

## Propósito

El Sistema de Gestión de Licitaciones permite a una organización administrar el
ciclo completo de una licitación pública o privada: registro de proveedores,
publicación de licitaciones con presupuesto en colones costarricenses (CRC),
recepción y validación de ofertas económicas, determinación automática de la
mejor oferta con su clasificación de ahorro, asignación del nivel de aprobación
según el monto, y visualización referencial de montos en dólares (USD).

## Problema que resuelve

La adjudicación manual de licitaciones es propensa a errores: ofertas duplicadas,
ofertas recibidas fuera de plazo, montos que superan el presupuesto, y criterios
de aprobación aplicados de forma inconsistente. El sistema automatiza estas
validaciones y deja trazabilidad verificable de cada decisión.

## Usuarios y roles del proceso XP

| Rol XP | Persona |
|---|---|
| Cliente | Estudiante (define prioridades y valida criterios de aceptación; el docente actúa como cliente final en la defensa). |
| Programador | Estudiante (modalidad individual). |

## Alcance funcional

1. **Proveedores**: CRUD con nombre único normalizado (Unicode, espacios, mayúsculas).
2. **Licitaciones**: CRUD con código único, presupuesto en CRC, fecha de cierre con calendario y ciclo de estados Borrador → Publicada → Cerrada.
3. **Ofertas**: registro validado (una por proveedor por licitación, monto ≤ presupuesto, licitación publicada y no vencida), consulta y filtrado.
4. **Mejor oferta**: menor monto CRC con desempate por orden de registro y clasificación de ahorro.
5. **Niveles de aprobación**: rangos parametrizables sin traslape que determinan el aprobador.
6. **Tipo de cambio**: administración local del tipo de cambio CRC/USD con un único registro activo; conversión visual sin alterar datos persistidos.
7. **Interfaz web**: landing page, navegación completa, modo claro/oscuro, alternancia CRC/USD, formato es-CR.
8. **API REST v1**: operaciones equivalentes con DTO, OpenAPI, paginación y ProblemDetails.

## Fuera de alcance

- Autenticación y autorización de usuarios (no requerida por el enunciado).
- Consulta de tipo de cambio en servicios externos en línea (el sistema opera sin Internet).
- Notificaciones por correo u otros canales.
- Firma digital o expediente electrónico de la licitación.

## Restricciones técnicas

- .NET 9, ASP.NET Core MVC + Web API, EF Core 9, PostgreSQL 16+.
- Montos en `decimal` con precisión `numeric(18,2)`; prohibido `float`/`double`.
- Fechas con `DateTimeOffset`, comparación en UTC, presentación en `America/Costa_Rica`.
- CRC es la única moneda persistida; USD es representación calculada.
- Docker Compose para ejecución local y Kubernetes para despliegue.
- Integración continua con GitHub Actions.

## Criterios de éxito

- Flujo funcional mínimo del enunciado (sección 5.3) ejecutable de extremo a extremo.
- Cobertura de líneas ≥ 80 % en Domain y Application, ≥ 70 % global.
- `docker compose up --build` inicia la solución sin pasos manuales complejos.
- Despliegue verificable en Kubernetes con persistencia tras reinicio.
- Historial de Git con iteraciones, TDD y liberaciones pequeñas evidenciadas.
