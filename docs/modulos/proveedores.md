# Módulo: Proveedores

Historias cubiertas: **HU-01** (registrar proveedor) y **HU-02** (administrar proveedores).

## Propósito

Mantener el padrón de empresas habilitadas para presentar ofertas, garantizando
que un mismo proveedor no se registre dos veces con distinta grafía.

## Responsabilidades

- Validar el nombre según los caracteres permitidos.
- Normalizar el nombre para comparar unicidad.
- Registrar auditoría (`CreatedAt`, `UpdatedAt`) y baja lógica (`DeletedAt`).

No decide nada sobre ofertas ni licitaciones: solo sabe si tiene ofertas
asociadas para elegir entre borrado físico y lógico.

## Dependencias

| Depende de | Para qué |
|---|---|
| `IReloj` | Sellos de auditoría deterministas en pruebas. |
| `IProveedorRepositorio` | Consultar unicidad y existencia de ofertas. |
| `IUnidadDeTrabajo` | Confirmar la transacción. |

## Entradas y salidas

| Operación | Entrada | Salida |
|---|---|---|
| Crear | `ProveedorEntrada(Nombre)` | `Resultado<ProveedorDto>` |
| Actualizar | `Guid`, `ProveedorEntrada` | `Resultado<ProveedorDto>` |
| Obtener | `Guid` | `Resultado<ProveedorDto>` |
| Listar | `ParametrosConsulta` | `PaginaResultado<ProveedorDto>` |
| Eliminar | `Guid` | `Resultado` |

## Reglas

### Caracteres permitidos (§8.4)

Letras, números, espacios, punto, coma y paréntesis. Expresión de referencia:
`^[\p{L}\p{N} .,\(\)]+$`. Cualquier otro símbolo se rechaza con
`PROVEEDOR_NOMBRE_CARACTERES_INVALIDOS`.

### Normalización para unicidad (§8.3)

`Normalizador.NormalizarNombreProveedor` aplica, en orden:

1. Recorte de espacios laterales.
2. Colapso de espacios repetidos a uno solo.
3. Normalización Unicode NFC.
4. Mayúsculas invariantes.

Así `"Empresa Central"`, `" empresa   central "` y `"EMPRESA CENTRAL"` producen
el mismo `NombreNormalizado` y se consideran el mismo proveedor.

**Decisión deliberada:** no se eliminan diacríticos. `"Mas"` y `"Más"` son
nombres distintos; tratarlos como duplicados impediría registrar proveedores
legítimos. El enunciado solo exige ignorar mayúsculas, espacios y forma Unicode.

El nombre que ve el usuario conserva su grafía original (con espacios ya
colapsados); el normalizado es solo para comparar.

### Unicidad en tres niveles

1. **Formulario** — pendiente, iteración 3.
2. **Servidor** — `ProveedorServicio` consulta `ExisteNombreAsync` antes de guardar.
3. **PostgreSQL** — índice único parcial `ix_proveedores_nombre_normalizado`.

El índice es **parcial** (`WHERE DeletedAt IS NULL`): tras dar de baja un
proveedor, su nombre queda libre para volver a usarse.

### Eliminación (§8.9)

| Situación | Comportamiento |
|---|---|
| Sin ofertas | Borrado físico; no hay evidencia que conservar. |
| Con ofertas | Borrado lógico; las ofertas se conservan y el proveedor desaparece de los listados. |

## Errores

| Código | Tipo | HTTP |
|---|---|---|
| `PROVEEDOR_NOMBRE_VACIO` | Validación | 400 |
| `PROVEEDOR_NOMBRE_CARACTERES_INVALIDOS` | Validación | 400 |
| `PROVEEDOR_NOMBRE_DUPLICADO` | Conflicto | 409 |
| `PROVEEDOR_NO_ENCONTRADO` | No encontrado | 404 |
| `CONFLICTO_CONCURRENCIA` | Concurrencia | 409 |

## Pruebas

- `ProveedorTests` — normalización, caracteres, auditoría, borrado lógico.
- `CasosDeUsoTests` — duplicidad, borrado físico frente a lógico.
- `ConsultasYPaginacionTests` — consultas, listados, exclusión de eliminados.
- `PersistenciaTests` — índice único real y reutilización del nombre tras la baja.
