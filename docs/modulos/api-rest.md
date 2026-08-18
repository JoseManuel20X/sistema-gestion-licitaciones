# Módulo: API REST

## Propósito

Exponer las operaciones del sistema a otros clientes mediante HTTP, con el mismo
comportamiento que la interfaz web. Cubre HU-13.

## Responsabilidades

- Traducir peticiones HTTP a casos de uso y sus resultados a códigos y cuerpos.
- Publicar el contrato en OpenAPI.
- **No contiene reglas de negocio.** Los controladores son delgados por
  construcción: cada acción invoca un caso de uso y traduce el resultado.

## Dependencias

`Licitaciones.Application` y `Licitaciones.Infrastructure`. Las entidades de
Entity Framework Core **no salen nunca** al exterior: se exponen los DTO de la
capa de aplicación, de modo que el modelo de datos pueda cambiar sin romper el
contrato publicado.

## Versionado

El prefijo de ruta `/api/v1` identifica la versión. Se descartó una biblioteca de
negociación de versiones porque hay una sola: añadir esa dependencia sería
complejidad especulativa, que es justo lo que el diseño simple de XP evita.
Cuando exista una v2, el prefijo permite servir ambas en paralelo.

## Traducción de errores

`ResultadoHttp` concentra la correspondencia entre el `TipoError` de la capa de
aplicación y el código HTTP. Al estar en un único lugar, la misma regla responde
siempre igual en todos los controladores, y añadir un error nuevo obliga a
clasificarlo.

| TipoError | HTTP | Significado |
|---|---|---|
| `Validacion` | 400 | Datos mal formados o ausentes |
| `NoEncontrado` | 404 | El recurso no existe |
| `Conflicto` | 409 | Choque con el estado actual, como un duplicado |
| `Concurrencia` | 409 | Otro proceso modificó el registro primero |
| `ReglaNegocio` | 422 | Datos correctos que infringen una regla |

## Forma de los errores

Todas las respuestas de error son `ProblemDetails` con dos extensiones propias:

```json
{
  "title": "Conflicto con el estado actual",
  "status": 409,
  "detail": "Ya existe un proveedor registrado con el nombre «empresa CENTRAL».",
  "instance": "/api/v1/proveedores",
  "codigoError": "PROVEEDOR_NOMBRE_DUPLICADO",
  "idCorrelacion": "0HNNQK37NK9TN:00000001"
}
```

`codigoError` permite reaccionar sin depender del texto del mensaje, que puede
cambiar o traducirse. `idCorrelacion` identifica la petición en los registros del
servidor.

## Seguridad de los errores

El §10.2 prohíbe exponer trazas, rutas internas, consultas o secretos.
`ManejadorExcepciones` convierte cualquier fallo imprevisto en un 500 con
`ProblemDetails`: el detalle real va al log del servidor junto al identificador de
correlación, y al cliente solo llega ese identificador.

Una prueba de integración comprueba explícitamente que el cuerpo de una respuesta
de error **no** contiene el nombre del índice, el proveedor de datos, la traza ni
la consulta SQL.

## Documentación interactiva

Swagger UI en `/swagger`, alimentado por la documentación XML de los
controladores, de modo que cada operación se describe una sola vez en el código.
El menú de la interfaz web enlaza a esta documentación.

## Pruebas

`ApiTests` levanta la API con `WebApplicationFactory` sobre el mismo contenedor
de PostgreSQL que el resto de pruebas de integración, así que verifica el camino
completo: petición HTTP, controlador, caso de uso, EF Core y base de datos.

Cubre los códigos correctos, la cabecera `Location` al crear (comprobando que
apunta a un recurso que existe de verdad), la paginación con totales, el contrato
publicado en `/swagger/v1/swagger.json` y la no filtración de detalles internos.

La lógica de negocio no se reprueba aquí: ya está cubierta por las pruebas
unitarias. En esta capa se verifica la traducción a HTTP, no la regla.

## Contrato completo

Los endpoints, sus parámetros y ejemplos de petición y respuesta están en
[api.md](../api.md).
