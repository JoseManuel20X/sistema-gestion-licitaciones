# Módulo: Niveles de aprobación

Historia cubierta: **HU-08** (parametrizar niveles de aprobación).

## Propósito

Determinar quién debe aprobar una adjudicación según su monto, mediante una tabla
que se administra desde la aplicación.

## Por qué una tabla y no condiciones fijas

El enunciado §8.7 lo exige explícitamente: «el aprobador debe obtenerse desde una
tabla parametrizable y no mediante una cadena fija de condiciones if/else».

El motivo es práctico: la política de aprobación de una organización cambia —se
añade un escalón, se ajusta un umbral por inflación— y con `if/else` cada cambio
obliga a recompilar y desplegar. Con la tabla, se edita un registro.

`TablaNivelesAprobacion.ResolverNivel` recorre los rangos y devuelve el primero
que contiene el monto. No hay ningún umbral escrito en el código.

## Tabla de referencia (dato semilla)

| Monto mínimo CRC | Monto máximo CRC | Aprobador |
|---|---|---|
| 0,01 | 999 999,99 | Encargado de área |
| 1 000 000,00 | 9 999 999,99 | Gerencia |
| 10 000 000,00 | Sin límite | Junta Directiva |

Se inserta con `DatosSemilla.SembrarAsync`, de forma idempotente.

## Reglas de consistencia

### Rangos inclusivos por ambos extremos

`Contiene(monto)` es verdadero si `monto >= MontoMinimoCRC` y
(`MontoMaximoCRC` es nulo o `monto <= MontoMaximoCRC`). Por eso 999 999,99 cae en
el primer rango y 1 000 000,00 en el segundo.

### Sin traslapes

Dos rangos se traslapan si `min1 <= max2 && min2 <= max1`, tratando el máximo
nulo como infinito. `GarantizarConsistencia` ordena los rangos por mínimo y
comprueba cada par consecutivo.

La validación se hace sobre **el conjunto completo tal como quedaría tras el
cambio**, no solo sobre el rango nuevo: al editar, el rango modificado se sustituye
en la lista y se valida el resultado. Así una edición no puede dejar la tabla
inconsistente.

### Un solo rango abierto

Como máximo un rango puede carecer de máximo. Con dos, un monto muy alto caería en
ambos y el aprobador dependería del orden de la consulta.

### Se admiten huecos

El enunciado prohíbe el traslape, no exige contigüidad. Un monto que cae en un
hueco no tiene aprobador: `ResolverNivel` devuelve `null` y el caso de uso
responde `APROBACION_SIN_NIVEL_APLICABLE` (regla de negocio, HTTP 422). Es
preferible un error explícito a asignar un aprobador arbitrario.

## Validaciones de cada nivel

| Regla | Código |
|---|---|
| Monto mínimo > 0 | `APROBACION_RANGO_INVALIDO` |
| Máximo nulo o mayor que el mínimo | `APROBACION_RANGO_INVALIDO` |
| Aprobador no vacío | `APROBACION_APROBADOR_VACIO` |
| Sin traslape con los demás | `APROBACION_RANGO_TRASLAPADO` |
| Un solo rango abierto | `APROBACION_RANGO_ABIERTO_DUPLICADO` |

En la base de datos: `ck_niveles_minimo_positivo`, `ck_niveles_rango_coherente` e
índice único `ix_niveles_aprobacion_minimo`. El índice no cubre todo el traslape
—eso lo valida el dominio— pero descarta el caso más común de dos rangos que
inician en el mismo monto.

## Relación con otros módulos

[Licitaciones](licitaciones.md) consulta este módulo al calcular la mejor oferta,
para devolver el aprobador que corresponde a su monto.

## Pruebas

- `NivelAprobacionTests` — resolución por tabla en los ocho montos límite,
  extremos inclusivos, traslapes, doble rango abierto, huecos.
- `CasosDeUsoTests` — creación y edición que provocan traslape.
- `ConsultasYPaginacionTests` — listado ordenado, consulta, edición, borrado.
- `PersistenciaTests` — semilla idempotente con los tres rangos.
