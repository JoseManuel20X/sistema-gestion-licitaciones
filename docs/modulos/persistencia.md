# Módulo: Persistencia

Capa `Licitaciones.Infrastructure`. Implementa los puertos que declara
`Licitaciones.Application` usando Entity Framework Core 9 sobre PostgreSQL 16.

El detalle del esquema está en [modelo-datos.md](../modelo-datos.md); aquí se
documenta el módulo como componente.

## Propósito

Aislar por completo el acceso a datos. Ninguna capa superior sabe que existe
Entity Framework Core: `Application` declara interfaces y recibe implementaciones
por inyección de dependencias.

## Componentes

| Componente | Responsabilidad |
|---|---|
| `LicitacionesDbContext` | Contexto. Solo declara los `DbSet` y aplica las configuraciones del ensamblado. |
| `Configuraciones/*` | Una clase por entidad: tablas, columnas, tipos, índices y restricciones. |
| `Repositorios/*` | Implementan los puertos: consultas, filtros, paginación. |
| `UnidadDeTrabajo` | Confirma cambios y **traduce los errores de PostgreSQL**. |
| `DatosSemilla` | Inserta niveles de aprobación y tipo de cambio inicial, de forma idempotente. |
| `RelojSistema` | Implementa `IReloj` con la hora real en UTC. |
| `FabricaDbContextDisenio` | Permite generar migraciones sin proyecto de arranque. |

## Traducción de errores: el punto clave

`UnidadDeTrabajo` es el **único lugar de la solución que conoce los códigos de
error de PostgreSQL**. Gracias a eso, la capa de aplicación puede distinguir un
duplicado de un fallo genérico sin depender del motor.

| SQLSTATE | Significado | Se traduce a |
|---|---|---|
| `23505` | Violación de restricción única | `ExcepcionConflictoPersistencia` con el código concreto según el índice |
| `23503` | Violación de clave foránea | `ExcepcionConflictoPersistencia` (`VIOLACION_INTEGRIDAD`) |
| `23514` | Violación de CHECK | `ExcepcionConflictoPersistencia` (`VIOLACION_INTEGRIDAD`) |
| — | `DbUpdateConcurrencyException` | `ExcepcionConcurrencia` |

El nombre del índice violado se mapea al código de negocio correspondiente:

```
ix_proveedores_nombre_normalizado  → PROVEEDOR_NOMBRE_DUPLICADO
ix_licitaciones_codigo_normalizado → LICITACION_CODIGO_DUPLICADO
ix_ofertas_licitacion_proveedor    → OFERTA_DUPLICADA
```

Los mensajes que se devuelven **no exponen** nombres de restricción, rutas
internas ni consultas, como exige §10.2. El nombre del índice se usa para
clasificar, no para mostrar.

## Consultas y paginación

Todos los listados aceptan `ParametrosConsulta` (página, tamaño, filtro,
ordenamiento). El tamaño de página se acota a 100 para que un cliente no pueda
pedir la tabla completa.

Los proveedores y licitaciones borrados lógicamente se excluyen **filtrando de
forma explícita** en cada consulta, en lugar de usar un filtro global de EF Core.
Es más código, pero el comportamiento queda a la vista en la consulta y no
depende de una configuración remota que alguien podría desactivar sin notarlo.

El filtro por título usa `EF.Functions.ILike`, la comparación sin distinción de
mayúsculas nativa de PostgreSQL, que se traduce a SQL. Los comodines `%` y `_`
que escriba la persona usuaria se neutralizan para que se busquen como caracteres
literales.

## Transacciones

`EnTransaccionAsync` envuelve la operación completa y se ejecuta a través de la
estrategia de reintentos de EF Core (`EnableRetryOnFailure`), necesaria porque un
reintento debe repetir la transacción entera, no una parte.

## Migraciones

```bash
# Generar
dotnet dotnet-ef migrations add NombreDeLaMigracion --project src/Licitaciones.Infrastructure --output-dir Persistencia/Migraciones

# Comprobar que el modelo no cambió sin migrar
dotnet dotnet-ef migrations has-pending-model-changes --project src/Licitaciones.Infrastructure
```

La segunda comprobación también se hace desde una prueba de integración, para que
el olvido se detecte en CI y no en la defensa.

`MigrarYSembrarAsync` aplica migraciones y siembra al arrancar, de modo que
`docker compose up --build` deje el sistema listo sin pasos manuales. En
Kubernetes conviene ejecutarlo desde un Job o un `initContainer` para que no
compitan varias réplicas.

## Configuración

La cadena de conexión llega por variable de entorno o secreto. **No se versiona
ninguna credencial real** (§11). En tiempo de diseño se lee
`LICITACIONES_CONNECTION`.

## Pruebas

`Licitaciones.IntegrationTests`, contra PostgreSQL 16 real en contenedor. Ver
[pruebas.md](../pruebas.md).
