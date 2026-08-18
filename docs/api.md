# API REST v1

Contrato HTTP del sistema. Todas las rutas cuelgan de `/api/v1` y devuelven
`application/json`. Las decisiones de diseño están en
[modulos/api-rest.md](modulos/api-rest.md).

## Documentación interactiva

Con la solución en marcha, `http://localhost:8080/swagger`. El contrato en crudo
está en `/swagger/v1/swagger.json`.

## Convenciones

| Aspecto | Regla |
|---|---|
| Identificadores | UUID v7 generados por el sistema, no editables |
| Montos | `decimal` con dos decimales, siempre en colones |
| Fechas | ISO 8601 con desplazamiento; se comparan en UTC |
| Enumerados | Viajan como texto (`"Publicada"`), no como número |
| Errores | `ProblemDetails` con `codigoError` e `idCorrelacion` |

### Paginación

Los listados aceptan `pagina`, `tamanoPagina` (máximo 100), `filtro`,
`ordenarPor` y `descendente`, y responden:

```json
{
  "elementos": [],
  "pagina": 1,
  "tamanoPagina": 20,
  "totalElementos": 42,
  "totalPaginas": 3,
  "tienePaginaAnterior": false,
  "tienePaginaSiguiente": true
}
```

## Licitaciones

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/licitaciones` | Listado paginado |
| GET | `/api/v1/licitaciones/{id}` | Detalle |
| POST | `/api/v1/licitaciones` | Crear en estado Borrador |
| PUT | `/api/v1/licitaciones/{id}` | Actualizar |
| PATCH | `/api/v1/licitaciones/{id}/estado` | Publicar o cerrar |
| DELETE | `/api/v1/licitaciones/{id}` | Eliminar |
| GET | `/api/v1/licitaciones/{id}/ofertas` | Ofertas de la licitación |
| POST | `/api/v1/licitaciones/{id}/ofertas` | Registrar oferta |
| GET | `/api/v1/licitaciones/{id}/mejor-oferta` | Mejor oferta, ahorro y aprobador |

### Crear una licitación

```bash
curl -X POST http://localhost:8080/api/v1/licitaciones \
  -H "Content-Type: application/json" \
  -d '{"codigo":"LIC-2026-001","titulo":"Compra de equipo","presupuestoEstimadoCRC":1000000,"fechaCierre":"2026-12-31T17:00:00-06:00"}'
```

`201 Created`, con `Location` apuntando al recurso:

```json
{
  "id": "01a0138a-fd2d-7a2d-afbb-e8362cd9db1e",
  "codigo": "LIC-2026-001",
  "titulo": "Compra de equipo",
  "estado": "Borrador",
  "estadoEfectivo": "Borrador",
  "fechaCierre": "2026-12-31T23:00:00+00:00",
  "presupuestoEstimadoCRC": 1000000.00,
  "vencida": false,
  "aceptaOfertas": false
}
```

`estado` es el campo persistido y `estadoEfectivo` el real: una licitación
publicada cuya fecha de cierre ya pasó devuelve `"Publicada"` y `"Cerrada"`
respectivamente, con `vencida: true` (§8.1).

### Cambiar el estado

```bash
curl -X PATCH http://localhost:8080/api/v1/licitaciones/{id}/estado \
  -H "Content-Type: application/json" \
  -d '{"transicion":"Publicar"}'
```

Valores admitidos: `Publicar` y `Cerrar`. Una transición prohibida devuelve `422`
con `codigoError: "LICITACION_TRANSICION_INVALIDA"`.

### Consultar la mejor oferta

```bash
curl http://localhost:8080/api/v1/licitaciones/{id}/mejor-oferta
```

```json
{
  "licitacionId": "01a0138a-fd2d-7a2d-afbb-e8362cd9db1e",
  "presupuestoEstimadoCRC": 1000000.00,
  "montoMejorOfertaCRC": 800000.00,
  "nombreProveedor": "Empresa Central",
  "porcentajeAhorro": 20.00,
  "clasificacion": "Oferta conveniente",
  "aprobador": "Encargado de área"
}
```

Sin ofertas devuelve `200` con `mejorOfertaId: null` y
`clasificacion: "Sin ofertas válidas"`.

## Proveedores

| Método | Ruta |
|---|---|
| GET | `/api/v1/proveedores` |
| GET | `/api/v1/proveedores/{id}` |
| POST | `/api/v1/proveedores` |
| PUT | `/api/v1/proveedores/{id}` |
| DELETE | `/api/v1/proveedores/{id}` |

```bash
curl -X POST http://localhost:8080/api/v1/proveedores \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Empresa Central"}'
```

Un segundo intento con `"  empresa   CENTRAL  "` devuelve `409`: la unicidad
ignora mayúsculas y espacios repetidos.

## Ofertas

| Método | Ruta |
|---|---|
| GET | `/api/v1/ofertas?licitacionId=&proveedorId=` |
| GET | `/api/v1/ofertas/{id}` |
| PUT | `/api/v1/ofertas/{id}` |
| DELETE | `/api/v1/ofertas/{id}` |

El alta se hace desde `POST /api/v1/licitaciones/{id}/ofertas`, porque una oferta
solo existe dentro de una licitación.

Rechazos habituales:

| Situación | HTTP | `codigoError` |
|---|---|---|
| Supera el presupuesto | 422 | `OFERTA_SUPERA_PRESUPUESTO` |
| Segunda del mismo proveedor | 409 | `OFERTA_DUPLICADA` |
| Licitación no publicada | 422 | `OFERTA_LICITACION_NO_PUBLICADA` |
| Licitación vencida | 422 | `OFERTA_LICITACION_VENCIDA` |

## Niveles de aprobación

| Método | Ruta |
|---|---|
| GET | `/api/v1/niveles-aprobacion` |
| GET | `/api/v1/niveles-aprobacion/{id}` |
| GET | `/api/v1/niveles-aprobacion/aprobador?montoCRC=` |
| POST | `/api/v1/niveles-aprobacion` |
| PUT | `/api/v1/niveles-aprobacion/{id}` |
| DELETE | `/api/v1/niveles-aprobacion/{id}` |

No se pagina: la tabla tiene unas pocas filas por definición. Un rango que se
traslape con otro devuelve `409`.

## Tipos de cambio

| Método | Ruta |
|---|---|
| GET | `/api/v1/tipos-cambio` |
| GET | `/api/v1/tipos-cambio/activo` |
| GET | `/api/v1/tipos-cambio/convertir?montoCRC=` |
| GET | `/api/v1/tipos-cambio/{id}` |
| POST | `/api/v1/tipos-cambio` |
| PUT | `/api/v1/tipos-cambio/{id}` |
| PATCH | `/api/v1/tipos-cambio/{id}/activar` |
| DELETE | `/api/v1/tipos-cambio/{id}` |

```bash
curl "http://localhost:8080/api/v1/tipos-cambio/convertir?montoCRC=1040000"
```

```json
{
  "montoCRC": 1040000.00,
  "montoUSD": 2000.00,
  "crCporUSD": 520.0000,
  "fechaVigencia": "2026-08-15T00:00:00+00:00"
}
```

## Colección reproducible de peticiones

El archivo [`Licitaciones.Api.http`](../src/Licitaciones.Api/Licitaciones.Api.http)
contiene el flujo completo del §5.3 en orden, ejecutable desde Visual Studio, VS
Code con la extensión REST Client o `curl`.

## Códigos de respuesta

| Código | Cuándo |
|---|---|
| 200 | Consulta o actualización correcta |
| 201 | Recurso creado, con `Location` |
| 204 | Eliminación correcta |
| 400 | Datos mal formados |
| 404 | Recurso inexistente |
| 409 | Duplicado o conflicto de concurrencia |
| 422 | Regla de negocio incumplida |
| 500 | Error inesperado, controlado y sin detalles internos |
