# Estrategia de pruebas

## Estado actual

| Proyecto | Pruebas | Estado |
|---|---|---|
| `Licitaciones.UnitTests` | 183 | ✅ Verdes |
| `Licitaciones.IntegrationTests` | 13 | ✅ Verdes (PostgreSQL 16 real) |
| `Licitaciones.FunctionalTests` | 0 | ⏳ Iteración 4 (HU-17) |

Cobertura de líneas medida con Coverlet:

| Capa | Cobertura | Mínimo exigido (§12.4) |
|---|---|---|
| `Licitaciones.Domain` | **94,7 %** | 80 % |
| `Licitaciones.Application` | **84,3 %** | 80 % |

La cobertura numérica no sustituye la calidad de los escenarios: los casos límite
(el instante exacto del cierre, el umbral exacto del 10 % de ahorro, el empate de
ofertas) están probados explícitamente.

## Cómo ejecutarlas

```bash
dotnet test
```

Las pruebas de integración necesitan Docker en ejecución: Testcontainers levanta
`postgres:16-alpine` automáticamente. Sin Docker, esas pruebas fallan al arrancar.

Solo las unitarias, sin Docker:

```bash
dotnet test tests/Licitaciones.UnitTests
```

Con cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults
```

## Pirámide de pruebas

### Unitarias — `Licitaciones.UnitTests`

Cubren el dominio y los casos de uso. No tocan disco ni red, por lo que corren en
milisegundos y pueden ejecutarse en cada ciclo TDD sin romper el ritmo.

| Archivo | Qué verifica |
|---|---|
| `Proveedores/ProveedorTests` | Normalización Unicode, colapso de espacios, caracteres permitidos (§8.4), unicidad del nombre normalizado, borrado lógico. |
| `Licitaciones/LicitacionTests` | Ciclo de estados completo, transiciones prohibidas, vencimiento en el instante exacto, presupuesto que no puede bajar de una oferta existente. |
| `Ofertas/OfertaTests` | Monto positivo, oferta igual al presupuesto (válida), oferta superior (rechazada), licitación no publicada, vencimiento. |
| `Ofertas/EvaluadorOfertasTests` | Mejor oferta, desempate por orden de registro, clasificación del ahorro y sus umbrales exactos. |
| `Aprobaciones/NivelAprobacionTests` | Resolución del aprobador por tabla, rangos sin traslape, un único rango abierto, huecos permitidos. |
| `TiposCambio/TipoCambioTests` | Conversión CRC→USD, tipo de cambio positivo, activación. |
| `Aplicacion/CasosDeUsoTests` | Duplicidad de proveedor, código único, oferta duplicada, mejor oferta con aprobador. |
| `Aplicacion/ConsultasYPaginacionTests` | Consultas, listados paginados, edición y borrado; acotamiento de la paginación. |
| `Aplicacion/FallosDePersistenciaTests` | Que un conflicto o una colisión de concurrencia se traduzcan a un resultado controlado y no escapen como excepción. |

**Dobles usados.** `RelojFalso` fija el instante actual. `RepositoriosFalsos`
implementa los puertos en memoria; verifica la orquestación —comprobar
duplicados, resolver el aprobador, traducir errores— sin arrancar una base de
datos. Lo que depende del motor se prueba aparte contra PostgreSQL real.

### Integración — `Licitaciones.IntegrationTests`

PostgreSQL 16 real en contenedor mediante Testcontainers, como exige §12.2. El
contenedor se comparte en la colección porque arrancarlo es lento; cada prueba
limpia sus datos con `TRUNCATE`.

Se aplican las **migraciones reales**, no `EnsureCreated`, para que la prueba
verifique también que las migraciones versionadas funcionan.

Qué se comprueba y por qué no basta una prueba unitaria:

| Escenario | Por qué necesita base de datos real |
|---|---|
| Índice único rechaza nombre equivalente | El índice es del motor; una prueba en memoria comprobaría la validación de la aplicación, no la última defensa. |
| El nombre se libera tras el borrado lógico | Depende del índice **parcial** `WHERE DeletedAt IS NULL`, que no existe en un proveedor en memoria. |
| Índice compuesto rechaza oferta duplicada | Igual: la restricción vive en PostgreSQL. |
| Clave foránea impide borrar licitación con ofertas | El comportamiento `RESTRICT` lo aplica el motor. |
| CHECK rechaza presupuesto negativo | Se inserta con **SQL crudo**, saltándose el dominio a propósito, para probar que la base rechaza el dato aunque la aplicación fallara. |
| Precisión decimal sin error de coma flotante | Verifica que `numeric(18,2)` conserva el valor exacto. |
| Concurrencia optimista con `xmin` | Dos contextos leen y escriben la misma fila; solo PostgreSQL mantiene `xmin`. |
| Semilla idempotente y un solo tipo de cambio activo | Comprueba el índice único parcial `WHERE Activo = true`. |
| Migraciones sin cambios pendientes | Detecta que alguien tocó el modelo sin generar migración. |

### Funcionales de extremo a extremo — pendiente

`Licitaciones.FunctionalTests` está creado y vacío. Corresponde a la HU-17 en la
iteración 4, con Playwright contra la aplicación real. Los escenarios exigidos
por §12.3 están enumerados en los criterios de aceptación de la historia.

## Convención de nombres

`Metodo_Escenario_ResultadoEsperado`, en español. Ejemplo:

```
Registrar_DespuesDeLaFechaDeCierre_EsRechazado
```

Los guiones bajos separan las tres partes y hacen legible el informe de
ejecución. La regla de análisis CA1707 se relaja **solo** en `tests/`, mediante
`tests/Directory.Build.props`; en el código de producción sigue activa.

## Cómo se aplica TDD

El ciclo es prueba que falla → implementación mínima → refactorización, y queda
reflejado en el historial con commits `test:`, `feat:` y `refactor:` separados.

Un ejemplo real de este proyecto, registrado en la
[bitácora](bitacora-xp.md): la prueba
`ObtenerMejorOferta_DevuelveClasificacionYAprobadorDeLaTabla` falló porque el
servicio leía una colección de navegación que el repositorio en memoria no
poblaba. El diagnóstico no fue «arreglar el doble», sino que el diseño dependía de
que el ORM cargara datos relacionados —una trampa que en producción habría
devuelto «sin ofertas válidas» en silencio—. Se eliminó la colección y la
dependencia se hizo explícita. La prueba dirigió un cambio de diseño, que es
justamente lo que TDD debe producir.
