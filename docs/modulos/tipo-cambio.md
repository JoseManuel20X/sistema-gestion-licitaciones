# Módulo: Tipo de cambio

## Propósito

Administrar el tipo de cambio CRC/USD que usa el sistema para mostrar los montos
en dólares. Cubre HU-09 y el cálculo de HU-10.

## Regla central

El colón es la **única moneda persistida y la única fuente de verdad**. La vista
en dólares es una representación calculada que nunca modifica los valores
almacenados (§8.8). Cambiar el tipo de cambio no reescribe ni una sola oferta.

## Responsabilidades

- CRUD de tipos de cambio con valor, fecha de vigencia y bandera de activo.
- Garantizar que **solo uno esté activo** para la operación ordinaria.
- Convertir montos de colones a dólares informando qué tipo de cambio se aplicó.

## Entradas y salidas

| Operación | Entrada | Salida |
|---|---|---|
| `CrearAsync` | Valor CRC/USD, fecha de vigencia | `TipoCambioDto` |
| `ActualizarAsync` | Id, valor, fecha | `TipoCambioDto` |
| `ActivarAsync` | Id | `TipoCambioDto` con el anterior ya desactivado |
| `ObtenerActivoAsync` | — | `TipoCambioDto` o error controlado |
| `ConvertirAsync` | Monto en CRC | `MontoConvertidoDto` con ambas monedas y la fecha |
| `EliminarAsync` | Id | Éxito, o rechazo si es el activo |

## Reglas

1. El valor debe ser mayor que cero.
2. Se guarda con **cuatro decimales**, no dos: un tipo de cambio se cotiza con más
   precisión que un monto, y redondearlo distorsionaría conversiones grandes.
3. **El primer registro se activa automáticamente.** Sin activo no hay conversión
   posible, y crearlo inactivo dejaría la aplicación inútil hasta un segundo paso
   manual sin ganar nada a cambio.
4. Los siguientes se crean inactivos.
5. **No se puede eliminar el activo**: dejaría al sistema sin poder convertir.
   Primero hay que activar otro.
6. `Monto USD = Monto CRC / CRCporUSD`, redondeado a dos decimales solo para
   presentar.

## La activación es transaccional

Es la decisión más importante del módulo. PostgreSQL tiene un índice único
parcial que solo admite una fila con `Activo = true`:

```
ix_tipos_cambio_activo_unico  UNIQUE (Activo) WHERE Activo = true
```

Por eso `ActivarAsync` desactiva el anterior y confirma **antes** de activar el
nuevo, todo dentro de una transacción. Si se hiciera al revés, o sin transacción,
habría un instante con dos filas activas y la base rechazaría la escritura; o
peor, quedaría sin ninguna activa.

La regla la impone el motor, no la aplicación: dos peticiones simultáneas no
pueden dejar dos activos aunque ambas pasen la comprobación previa.

## Errores

| Código | Situación | HTTP |
|---|---|---|
| `TIPO_CAMBIO_NO_POSITIVO` | Valor menor o igual que cero | 400 |
| `TIPO_CAMBIO_NO_ENCONTRADO` | Id inexistente | 404 |
| `TIPO_CAMBIO_SIN_ACTIVO` | No hay ninguno activo al convertir | 422 |
| `TIPO_CAMBIO_ACTIVO_NO_ELIMINABLE` | Se intenta borrar el activo | 422 |
| `CONFLICTO_CONCURRENCIA` | Otro usuario cambió el activo primero | 409 |

## Funcionamiento sin Internet

El valor se administra localmente y no se consulta a ningún servicio externo, de
modo que el sistema opera sin conexión, como exige el §8.8. La semilla deja un
tipo de cambio inicial activo para que la aplicación sea utilizable desde el
primer arranque.

## Pruebas

`TipoCambioServicioTests` cubre la activación transaccional (verificando que
ocurre dentro de una sola transacción), la activación del ya activo, el primer
registro que se activa solo, el rechazo al eliminar el activo, la conversión con
su fecha de vigencia y el orden del listado. `TipoCambioTests` cubre la entidad:
conversión, valor positivo y que convertir no altera el monto original.
