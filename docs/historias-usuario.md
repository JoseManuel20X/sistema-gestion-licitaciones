# Historias de usuario

Historias definidas en el Planning Game inicial. La prioridad la asigna el
cliente (Alta/Media/Baja) y la estimación se expresa en puntos (1 punto ≈ media
jornada ideal de trabajo). Cada historia se vincula con sus pruebas y commits
desde la [bitácora XP](bitacora-xp.md) al cerrarse.

## Resumen

| ID | Historia | Prioridad | Puntos | Iteración |
|---|---|---|---|---|
| HU-01 | Registrar proveedor | Alta | 2 | 1 |
| HU-02 | Administrar proveedores | Alta | 2 | 1 |
| HU-03 | Crear licitación | Alta | 3 | 1 |
| HU-04 | Publicar y cerrar licitación | Alta | 2 | 1 |
| HU-05 | Registrar oferta válida | Alta | 3 | 2 |
| HU-06 | Rechazar ofertas inválidas | Alta | 3 | 2 |
| HU-07 | Consultar mejor oferta y clasificación | Alta | 2 | 2 |
| HU-08 | Parametrizar niveles de aprobación | Media | 2 | 2 |
| HU-09 | Administrar tipos de cambio | Media | 2 | 3 |
| HU-10 | Alternar montos CRC/USD | Media | 1 | 3 |
| HU-11 | Landing page y navegación | Media | 2 | 3 |
| HU-12 | Modo claro y modo oscuro | Baja | 1 | 3 |
| HU-13 | API REST v1 documentada | Alta | 3 | 3 |
| HU-14 | Ejecutar con Docker Compose | Alta | 2 | 4 |
| HU-15 | Desplegar en Kubernetes | Alta | 3 | 4 |
| HU-16 | Integración continua en GitHub Actions | Alta | 2 | 4 |
| HU-17 | Pruebas funcionales de extremo a extremo | Alta | 3 | 4 |

**Total estimado: 38 puntos** distribuidos en 4 iteraciones (~9-10 puntos por iteración).

---

## Iteración 1 — Fundación, proveedores y licitaciones

### HU-01 Registrar proveedor

**Como** encargado de compras, **quiero** registrar proveedores con su nombre,
**para** que puedan participar en las licitaciones.

- Prioridad: Alta · Estimación: 2 puntos

**Criterios de aceptación**
1. El nombre admite letras, números, espacios, punto, coma y paréntesis; cualquier otro símbolo se rechaza con mensaje claro.
2. El nombre es único tras normalizar: recorte de espacios laterales, colapso de espacios repetidos, normalización Unicode y comparación sin distinguir mayúsculas ("Empresa Central", " empresa   central " y "EMPRESA CENTRAL" son el mismo proveedor).
3. La unicidad se valida en formulario, servidor y con índice único en PostgreSQL.
4. El identificador se genera automáticamente y no es editable.
5. Se registran `CreatedAt` y `UpdatedAt`.

### HU-02 Administrar proveedores

**Como** encargado de compras, **quiero** listar, consultar, editar y eliminar
proveedores, **para** mantener el padrón actualizado.

- Prioridad: Alta · Estimación: 2 puntos

**Criterios de aceptación**
1. El listado tiene paginación, filtro por nombre y ordenamiento.
2. La edición aplica las mismas validaciones de HU-01.
3. Un proveedor con ofertas no se elimina físicamente: se aplica borrado lógico (`DeletedAt`) y deja de aparecer en listados ordinarios.
4. Toda eliminación pide confirmación previa.
5. Desde el detalle se consultan sus ofertas relacionadas.

### HU-03 Crear licitación

**Como** encargado de compras, **quiero** crear licitaciones con código, título,
presupuesto y fecha de cierre, **para** convocar ofertas de proveedores.

- Prioridad: Alta · Estimación: 3 puntos

**Criterios de aceptación**
1. El código es único ignorando espacios laterales y mayúsculas/minúsculas; se valida en formulario, servidor y con índice único en PostgreSQL.
2. El presupuesto en CRC es mayor que cero, en `decimal` con precisión `numeric(18,2)`.
3. La fecha y hora de cierre se selecciona con control de calendario y hora, y debe ser futura al publicar.
4. La licitación nace en estado Borrador.
5. No puede reducirse el presupuesto por debajo de una oferta ya registrada.

### HU-04 Publicar y cerrar licitación

**Como** encargado de compras, **quiero** controlar el ciclo de estados de la
licitación, **para** que solo se reciban ofertas en el periodo válido.

- Prioridad: Alta · Estimación: 2 puntos

**Criterios de aceptación**
1. Transiciones permitidas: Borrador→Publicada (datos completos, presupuesto válido, cierre futuro), Borrador→Cerrada (cancelación documentada), Publicada→Cerrada (acción autorizada o cierre alcanzado).
2. Transiciones prohibidas: Publicada→Borrador y cualquier salida desde Cerrada.
3. Una licitación cuya fecha de cierre llegó se considera cerrada funcionalmente aunque el campo estado aún diga Publicada.
4. Cada transición inválida produce un mensaje de error claro, nunca una excepción sin controlar.

---

## Iteración 2 — Ofertas, mejor oferta y aprobación

### HU-05 Registrar oferta válida

**Como** proveedor, **quiero** registrar mi oferta económica en una licitación
publicada, **para** participar del concurso.

- Prioridad: Alta · Estimación: 3 puntos

**Criterios de aceptación**
1. La oferta se registra solo si la licitación está Publicada y su fecha de cierre no se alcanzó.
2. El monto en CRC es mayor que cero y menor o igual al presupuesto (igual al presupuesto es válido).
3. Se registra la fecha y hora de la oferta (`FechaRegistro`) en UTC.
4. Las ofertas se pueden listar y filtrar por licitación y por proveedor.

### HU-06 Rechazar ofertas inválidas

**Como** encargado de compras, **quiero** que el sistema rechace ofertas
inválidas con mensajes claros, **para** garantizar la integridad del concurso.

- Prioridad: Alta · Estimación: 3 puntos

**Criterios de aceptación**
1. Se rechaza una segunda oferta del mismo proveedor en la misma licitación (índice único compuesto LicitacionId + ProveedorId).
2. Se rechaza una oferta con monto superior al presupuesto.
3. Se rechaza una oferta cuando la fecha actual es igual o posterior al cierre.
4. Se rechaza una oferta sobre licitación en Borrador o Cerrada.
5. Las ofertas de licitaciones cerradas no pueden crearse, editarse ni eliminarse; se conservan como evidencia.
6. El reloj está abstraído en un servicio inyectable para probar el vencimiento de forma determinista.

### HU-07 Consultar mejor oferta y clasificación

**Como** encargado de compras, **quiero** conocer la mejor oferta, su ahorro y
el aprobador correspondiente, **para** adjudicar la licitación.

- Prioridad: Alta · Estimación: 2 puntos

**Criterios de aceptación**
1. La mejor oferta es la válida de menor monto CRC; en empate gana la registrada primero.
2. Sin ofertas se muestra "Sin ofertas válidas".
3. Ahorro = ((Presupuesto − Mejor oferta) / Presupuesto) × 100. Clasificación: ≥ 10 % "Oferta conveniente"; > 0 % y < 10 % "Oferta aceptable"; 0 % "Oferta válida sin ahorro".
4. La consulta muestra también el nivel de aprobación que corresponde al monto.

### HU-08 Parametrizar niveles de aprobación

**Como** administrador, **quiero** configurar rangos de montos con su aprobador,
**para** que la aprobación no dependa de código fijo.

- Prioridad: Media · Estimación: 2 puntos

**Criterios de aceptación**
1. CRUD de niveles con monto mínimo, monto máximo (opcional) y aprobador.
2. Los rangos no pueden traslaparse; solo puede existir un rango abierto (sin máximo).
3. El aprobador se resuelve consultando la tabla, sin cadenas if/else fijas.
4. Datos semilla: 0,01–999 999,99 Encargado de área; 1 000 000,00–9 999 999,99 Gerencia; 10 000 000,00 sin límite Junta Directiva.

---

## Iteración 3 — Moneda, interfaz y API

### HU-09 Administrar tipos de cambio

**Como** administrador, **quiero** mantener tipos de cambio CRC/USD con vigencia
y un único activo, **para** convertir montos sin depender de Internet.

- Prioridad: Media · Estimación: 2 puntos

**Criterios de aceptación**
1. CRUD de tipos de cambio con valor CRC por USD (> 0), fecha de vigencia y bandera de activo.
2. Solo un registro puede estar activo; activar uno desactiva el anterior en la misma transacción.
3. Existe un dato semilla con un tipo de cambio inicial activo.

### HU-10 Alternar montos CRC/USD

**Como** usuario, **quiero** alternar la visualización de montos entre CRC y
USD, **para** interpretar los valores en ambas monedas.

- Prioridad: Media · Estimación: 1 punto

**Criterios de aceptación**
1. Un botón visible alterna todos los montos de la vista entre CRC y USD.
2. Monto USD = Monto CRC / tipo de cambio activo; los valores persistidos nunca cambian.
3. Se muestra la fecha del tipo de cambio utilizado.
4. El formato monetario usa la cultura es-CR para colones.

### HU-11 Landing page y navegación

**Como** visitante, **quiero** una página inicial que explique el flujo de
licitación y un menú completo, **para** orientarme en el sistema.

- Prioridad: Media · Estimación: 2 puntos

**Criterios de aceptación**
1. La landing explica propósito, flujo de licitación, ofertas, mejor oferta, nivel de aprobación y conversión monetaria.
2. Menú con acceso a Inicio, Licitaciones, Proveedores, Ofertas, Niveles de aprobación, Tipo de cambio y documentación de la API.
3. Diseño adaptable a computadora y móvil.
4. Los recursos front-end están incluidos localmente; la interfaz no depende de una CDN.

### HU-12 Modo claro y modo oscuro

**Como** usuario, **quiero** cambiar entre modo claro y oscuro con persistencia,
**para** usar el sistema con comodidad visual.

- Prioridad: Baja · Estimación: 1 punto

**Criterios de aceptación**
1. Control visible en toda la aplicación para alternar el tema.
2. La preferencia persiste entre visitas (almacenamiento local del navegador).
3. Ambos temas mantienen contraste legible en tablas, formularios y mensajes.

### HU-13 API REST v1 documentada

**Como** integrador, **quiero** una API REST versionada y documentada, **para**
operar el sistema desde otros clientes.

- Prioridad: Alta · Estimación: 3 puntos

**Criterios de aceptación**
1. Endpoints mínimos del enunciado (sección 10.1) bajo `/api/v1`, con DTO y sin exponer entidades de EF Core.
2. Listados con paginación, filtrado y ordenamiento.
3. Códigos HTTP correctos (200/201/204/400/404/409/422/500 controlado) y ProblemDetails con título, estado, detalle seguro, código de error e identificador de correlación.
4. Documentación OpenAPI/Swagger navegable desde el menú.
5. Colección reproducible de solicitudes documentada en `/docs/api.md`.

---

## Iteración 4 — Despliegue, CI y pruebas E2E

### HU-14 Ejecutar con Docker Compose

**Como** evaluador, **quiero** iniciar la solución con `docker compose up
--build`, **para** ejecutarla sin pasos manuales complejos.

- Prioridad: Alta · Estimación: 2 puntos

**Criterios de aceptación**
1. Dockerfile multi-stage para .NET 9 con usuario no privilegiado.
2. Servicios de aplicación y PostgreSQL con volumen persistente, variables de entorno y health checks.
3. Los datos persisten después de reiniciar los contenedores.

### HU-15 Desplegar en Kubernetes

**Como** operador, **quiero** desplegar la solución en Kubernetes, **para**
ejecutarla de forma escalable y con persistencia.

- Prioridad: Alta · Estimación: 3 puntos

**Criterios de aceptación**
1. Manifiestos de `/k8s`: namespace, Deployment + Service de la aplicación, StatefulSet + Service + PVC de PostgreSQL, ConfigMap y Secret de ejemplo.
2. Probes de startup, readiness y liveness; solicitudes y límites de recursos.
3. Migraciones ejecutadas de forma controlada.
4. Evidencia en `/docs/kubernetes.md` de pods, servicios, PVC, logs y datos conservados tras reinicio.

### HU-16 Integración continua en GitHub Actions

**Como** equipo, **quiero** un flujo de CI que compile, pruebe y valide cada
cambio, **para** integrar de forma continua y segura.

- Prioridad: Alta · Estimación: 2 puntos

**Criterios de aceptación**
1. El flujo restaura, compila, ejecuta pruebas con cobertura, verifica formato y análisis estático.
2. Construye la imagen Docker, valida los manifiestos de Kubernetes y revisa dependencias vulnerables.
3. Un fallo del flujo bloquea la integración del cambio.

### HU-17 Pruebas funcionales de extremo a extremo

**Como** cliente, **quiero** pruebas automatizadas de navegador del flujo
completo, **para** verificar las historias desde la interfaz real.

- Prioridad: Alta · Estimación: 3 puntos

**Criterios de aceptación**
1. Cubren: landing y navegación, CRUD de proveedor y licitación, publicación y cierre, registro y rechazo de ofertas, modo claro/oscuro, conversión CRC/USD y mensajes de validación.
2. Se ejecutan con Playwright contra la aplicación real.
3. Son reproducibles localmente y en CI.
