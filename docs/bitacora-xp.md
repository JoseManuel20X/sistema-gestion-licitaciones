# Bitácora XP

Registro de resultados por iteración: historias cerradas, velocidad observada,
evidencia de TDD, refactorizaciones, liberaciones y retroalimentación del
cliente. Se actualiza durante cada iteración, no al final del proyecto.

## Planning Game inicial — 2026-07-14

- Se definió la [visión y alcance](vision-alcance.md) a partir del enunciado del proyecto.
- Se redactaron 17 historias de usuario con prioridad, estimación y criterios de aceptación ([historias-usuario.md](historias-usuario.md)).
- Se acordó el plan de liberación en 4 iteraciones de una semana ([plan-xp.md](plan-xp.md)).
- Velocidad planificada: 9-10 puntos por iteración.
- Decisiones del cliente: comenzar por proveedores y licitaciones (dependencias de todo lo demás); dejar despliegue y E2E para la iteración final manteniendo Docker utilizable desde la iteración 1 para PostgreSQL local.

---

## Iteración 1 — Fundación, proveedores y licitaciones

**Historias comprometidas:** HU-01, HU-02, HU-03, HU-04 (9 puntos)

### Registro de trabajo

**Configuración de calidad.** Antes de la primera historia se centralizó la
configuración en `Directory.Build.props` con `TreatWarningsAsErrors` y
`EnforceCodeStyleInBuild`. Decisión del cliente: es preferible que una advertencia
rompa la compilación desde el primer día a acumular deuda que después haya que
limpiar en bloque.

Las reglas de análisis que chocan con las convenciones acordadas se desactivaron
**una por una y con justificación escrita**, no en bloque:

- `CA1710` (sufijo `Exception` en inglés): el vocabulario del dominio es español
  por acuerdo del plan XP.
- `CA1707` (guiones bajos): solo en `tests/`, donde la convención
  `Metodo_Escenario_Resultado` hace legible el informe de ejecución.
- `CA1000` en `Resultado<T>`: fábricas estáticas genéricas, suprimido con atributo
  local y justificación.

**HU-01 y HU-02 (proveedores).** Ciclo TDD por cada regla. La normalización se
escribió primero como prueba con los tres ejemplos del enunciado
(`Empresa Central`, `empresa central`, `EMPRESA CENTRAL`) y luego se implementó
`Normalizador.NormalizarNombreProveedor`.

Decisión de diseño registrada: **no se eliminan diacríticos** al normalizar.
«Mas» y «Más» son nombres distintos y tratarlos como duplicados impediría
registrar proveedores legítimos. El enunciado solo exige ignorar mayúsculas,
espacios y forma Unicode.

**HU-03 y HU-04 (licitaciones).** El punto difícil fue el §8.1: una licitación
vencida «se considera cerrada funcionalmente aunque el campo estado diga
Publicada». Se resolvió separando `Estado` (persistido) de
`EstadoEfectivo(reloj)` (calculado). Alternativa descartada: un proceso que
recorra la tabla actualizando estados, que habría añadido una fuente de
inconsistencias y un componente que mantener.

**Refactorización — política de redondeo.** Al escribir la tercera entidad con
`decimal.Round(...)` repetido apareció duplicación. Se extrajo `Dinero.Redondear`
y, al hacerlo, se corrigió el criterio: de `ToEven` (bancario) a `AwayFromZero`
(comercial), que es el habitual en montos. Centralizarlo evita que dos capas
redondeen distinto y produzcan diferencias de un céntimo.

**Persistencia.** Migración inicial `20260813042242_MigracionInicial` con
`numeric(18,2)`, índices únicos parciales, restricciones CHECK y `xmin` como token
de concurrencia optimista. Se eligió `xmin` sobre una columna de versión propia
porque lo mantiene el motor en cada `UPDATE`, sin código que pueda olvidar
incrementarla; el §11 admite explícitamente «un mecanismo equivalente de
PostgreSQL».

### Cierre de iteración

- **Historias terminadas:** HU-01, HU-02, HU-03, HU-04.
- **Velocidad observada:** 9 puntos, igual a lo planificado.
- **Comparación con lo planificado:** sin desviación. La configuración de calidad
  costó más de lo previsto, compensado por lo directo que resultó el dominio al
  no depender de infraestructura.
- **Retroalimentación del cliente:** aceptados los criterios. Se pidió que la
  unicidad quedara verificada también contra PostgreSQL real y no solo en el
  servidor, lo que adelantó la configuración de Testcontainers.
- **Ajustes para la siguiente iteración:** mantener el patrón de prueba de
  integración por cada restricción de base de datos nueva.

---

## Iteración 2 — Ofertas, mejor oferta y aprobación

**Historias comprometidas:** HU-05, HU-06, HU-07, HU-08 (10 puntos)

### Registro de trabajo

**HU-05 y HU-06 (ofertas).** Los casos límite se escribieron como prueba antes de
implementar, porque son los que se implementan mal con facilidad:

- Oferta **igual** al presupuesto: válida. Solo se rechaza la que lo supera —un
  `>=` en lugar de `>` habría pasado inadvertido.
- Un *tick* antes del cierre: se acepta. En el instante exacto: se rechaza.

El reloj inyectable (`IReloj`) fue condición previa para que estas pruebas fueran
deterministas en lugar de intermitentes.

**HU-07 (mejor oferta).** `EvaluadorOfertas` se implementó como función pura sobre
datos ya cargados, de modo que toda la regla se prueba sin base de datos. El
desempate añade el identificador como último criterio para que el resultado sea
determinista incluso con dos ofertas de marca temporal idéntica.

**Refactorización dirigida por una prueba que falló.** La prueba
`ObtenerMejorOferta_DevuelveClasificacionYAprobadorDeLaTabla` falló devolviendo
«sin ofertas válidas». La causa no era el doble de prueba: el servicio leía
`licitacion.Ofertas`, una colección de navegación que solo estaba poblada si quien
llamaba había usado `Include`.

Es una trampa silenciosa —en producción habría devuelto un resultado incorrecto en
lugar de fallar—. Se eliminaron las colecciones `Ofertas` de `Licitacion` y
`Proveedor`, la relación se configuró desde el lado de `Oferta` y la dependencia
se hizo explícita inyectando `IOfertaRepositorio` en `LicitacionServicio`. Se
retiró también `ObtenerConOfertasAsync`, que quedó sin uso: diseño simple, sin
infraestructura especulativa.

La prueba dirigió un cambio de diseño, que es lo que TDD debe producir.

> **Nota sobre el historial.** Esta refactorización y la de la política de
> redondeo se resolvieron dentro de la sesión de trabajo, antes del primer
> commit de los archivos afectados, así que no aparecen como commits `refactor:`
> independientes: quedaron consolidadas en el `feat:` de cada módulo, cuyo
> cuerpo explica la decisión. Se registran aquí porque el razonamiento importa
> más que el número de commits, y porque son exactamente las decisiones que la
> defensa oral puede preguntar. A partir de la iteración 3 las refactorizaciones
> posteriores al primer commit de un archivo sí quedan como commits propios.

**HU-08 (niveles de aprobación).** La validación de traslape se hace sobre el
conjunto completo tal como quedaría tras el cambio, no solo sobre el rango nuevo;
si no, una edición podría dejar la tabla inconsistente. Se admiten huecos entre
rangos: el enunciado prohíbe el traslape, no exige contigüidad, y un monto en un
hueco produce un error explícito en vez de un aprobador arbitrario.

**Cobertura.** Tras la primera medición, `Application` quedó en 46 %. Se
completaron las pruebas de consultas, listados y —lo más valioso— de traducción de
fallos de persistencia, inyectando el fallo en el doble de `IUnidadDeTrabajo`.
Esas pruebas verifican que un choque de concurrencia llega al cliente como 409 y
no como 500.

### Cierre de iteración

- **Historias terminadas:** HU-05, HU-06, HU-07, HU-08.
- **Velocidad observada:** 10 puntos, igual a lo planificado.
- **Estado de la verificación:** 183 pruebas unitarias y 13 de integración en
  verde; compilación sin advertencias.
- **Cobertura:** `Domain` 94,7 % · `Application` 84,3 % (mínimo exigido: 80 %).
- **Retroalimentación del cliente:** aceptados los criterios. Se valoró
  especialmente que la refactorización de la colección de navegación quedara
  documentada, por ser el tipo de decisión que la defensa oral puede preguntar.
- **Ajustes para la siguiente iteración:** los casos de uso ya devuelven todo lo
  que la interfaz y la API necesitan (incluido `TipoError`, que fija el código
  HTTP), así que la iteración 3 es de presentación y no debería requerir cambios
  en el dominio.

---

## Iteración 3 — Moneda, interfaz y API

**Historias comprometidas:** HU-09, HU-10, HU-11, HU-12, HU-13 (9 puntos)

_(pendiente de iniciar)_

---

## Iteración 4 — Despliegue, CI y E2E

**Historias comprometidas:** HU-14, HU-15, HU-16, HU-17 (10 puntos)

_(pendiente de iniciar)_
