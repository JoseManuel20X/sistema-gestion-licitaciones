# Módulo: Ofertas

Historias cubiertas: **HU-05** (registrar oferta válida) y **HU-06** (rechazar ofertas inválidas).

## Propósito

Registrar las propuestas económicas de los proveedores garantizando que solo se
acepten dentro del periodo válido, sin duplicados y sin superar el presupuesto.

## Reglas de admisión

Una oferta se registra únicamente si se cumple **todo**:

| Regla | Verificación | Código de error |
|---|---|---|
| La licitación existe | Repositorio | `LICITACION_NO_ENCONTRADA` |
| El proveedor existe | Repositorio | `PROVEEDOR_NO_ENCONTRADO` |
| La licitación está publicada | Dominio | `OFERTA_LICITACION_NO_PUBLICADA` |
| No se alcanzó la fecha de cierre | Dominio + `IReloj` | `OFERTA_LICITACION_VENCIDA` |
| El proveedor no ofertó ya | Servicio + índice único | `OFERTA_DUPLICADA` |
| Monto mayor que cero | Dominio | `OFERTA_MONTO_NO_POSITIVO` |
| Monto ≤ presupuesto | Dominio | `OFERTA_SUPERA_PRESUPUESTO` |

### Vencimiento (§8.2)

El rechazo ocurre cuando la hora actual es **igual o posterior** a la fecha de
cierre. El instante exacto del cierre ya está vencido; un *tick* antes todavía se
acepta. Ambos casos límite están probados.

La comparación se hace en UTC a través de `IReloj`, que en pruebas es un doble
controlado. Sin esa abstracción, probar el vencimiento exigiría esperas reales y
produciría pruebas intermitentes.

### Oferta igual al presupuesto

**Es válida.** Solo se rechaza la que lo supera. Es un caso límite fácil de
implementar mal con un `>=` en lugar de un `>`, por lo que tiene prueba propia.

### Unicidad proveedor–licitación (§8.3)

Un proveedor no puede presentar dos ofertas en la misma licitación. Se comprueba
en el servicio (mensaje claro) y lo respalda el índice único compuesto
`ix_ofertas_licitacion_proveedor` sobre `(LicitacionId, ProveedorId)`.

La comprobación previa no basta: dos peticiones simultáneas pueden pasarla las
dos. Cuando el índice rechaza la segunda, `UnidadDeTrabajo` traduce el error de
PostgreSQL al mismo código `OFERTA_DUPLICADA`, de modo que el cliente recibe la
misma respuesta gane quien gane la carrera.

## Edición y eliminación (§8.9)

Ambas requieren que la licitación siga **publicada y vigente**. En cuanto cierra —
por acción o por vencimiento— las ofertas quedan congeladas y se conservan como
evidencia. No se aplica borrado lógico a las ofertas: o se pueden borrar
físicamente (licitación abierta) o no se pueden borrar en absoluto.

`FechaRegistro` **no cambia** al editar el monto; solo se actualiza `UpdatedAt`.
Esto importa porque el desempate de la mejor oferta usa `FechaRegistro`: editar
una oferta no debe permitir «adelantarse» en la cola.

## Entradas y salidas

| Operación | Entrada | Salida |
|---|---|---|
| Registrar | `Guid` licitación, `OfertaEntrada(ProveedorId, MontoOfertadoCRC)` | `Resultado<OfertaDto>` |
| Actualizar | `Guid`, `OfertaActualizacion(MontoOfertadoCRC)` | `Resultado<OfertaDto>` |
| Obtener | `Guid` | `Resultado<OfertaDto>` |
| Listar | `ParametrosConsulta`, filtros opcionales por licitación y proveedor | `PaginaResultado<OfertaDto>` |
| Eliminar | `Guid` | `Resultado` |

## Relación con otros módulos

- [Licitaciones](licitaciones.md) decide si se admiten ofertas
  (`GarantizarQueAceptaOfertas`) y consume la lista para calcular la mejor oferta.
- [Proveedores](proveedores.md) debe existir y no estar dado de baja.

## Pruebas

- `OfertaTests` — monto, presupuesto, estado, vencimiento y sus límites.
- `CasosDeUsoTests` — duplicidad, proveedor inexistente, oferta de otro proveedor.
- `ConsultasYPaginacionTests` — edición y borrado según el estado de la licitación.
- `PersistenciaTests` — índice único compuesto real.
