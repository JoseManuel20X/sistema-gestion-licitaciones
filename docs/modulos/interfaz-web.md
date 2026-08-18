# Módulo: Interfaz web

## Propósito

Presentar el sistema a las personas usuarias: navegación, formularios, listados
y las dos alternancias visuales que exige el enunciado (tema claro/oscuro y
CRC/USD). Cubre los criterios de interfaz de HU-01 a HU-12.

## Responsabilidades

- Mostrar y capturar datos. **No contiene ninguna regla de negocio**: cada acción
  invoca un caso de uso de `Licitaciones.Application` y traduce su resultado.
- Validar en el navegador antes de enviar, como conveniencia.
- Presentar los montos con formato `es-CR` y las fechas en la zona de Costa Rica.

## Dependencias

`Licitaciones.Application` (casos de uso y DTO) y `Licitaciones.Infrastructure`
(solo para el registro en el contenedor de dependencias). No conoce Entity
Framework Core ni PostgreSQL.

## Estructura

| Elemento | Función |
|---|---|
| `Controllers/` | Un controlador por módulo, todos delgados |
| `Models/Formularios.cs` | Modelos de formulario con anotaciones de validación |
| `Models/PaginacionViewModel.cs` | Datos del control de paginación |
| `Infraestructura/ControladorBase.cs` | Traducción de errores y mensajes |
| `Infraestructura/EnlazadorDecimal.cs` | Enlace de decimales tolerante al separador |
| `Views/Shared/_Layout.cshtml` | Navegación, tema, interruptor de moneda |
| `Views/Shared/_Paginacion.cshtml` | Control de paginación compartido |
| `Views/Shared/_Mensajes.cshtml` | Banner de éxito y error tras redirigir |

## Decisiones de diseño

**Dónde se muestra cada error.** `ControladorBase.RegistrarError` decide en un
solo lugar: los errores de validación y los conflictos van junto a su campo; las
reglas de negocio van como resumen, porque no pertenecen a un campo concreto.
Sin esa centralización cada controlador improvisaría su propio criterio.

**POST-Redirect-GET.** Toda escritura redirige tras guardar, así recargar la
página no repite la operación. El mensaje viaja en `TempData`, que sobrevive
exactamente a esa redirección.

**Validación duplicada a propósito.** Los modelos de formulario repiten algunas
reglas del dominio para poder validarlas sin ir al servidor. El dominio sigue
siendo la autoridad y vuelve a comprobarlas, con PostgreSQL detrás. Si alguna vez
discrepan, manda el dominio.

**Tema con `data-bs-theme`.** Se usa el mecanismo nativo de Bootstrap 5.3 en vez
de una paleta propia, de modo que ambos modos queden coherentes sin mantener dos
juegos de colores. Se aplica en el `<head>` antes de pintar: hacerlo al final del
documento produce un parpadeo blanco visible.

**Conversión de moneda solo de presentación.** Cada monto conserva su importe
original en `data-monto-crc` y se recalcula al vuelo. Los valores almacenados no
cambian nunca (§8.8). El tipo de cambio se pide una sola vez, la primera vez que
se alterna, en lugar de incrustarlo en cada página.

## Errores y casos límite tratados

| Situación | Comportamiento |
|---|---|
| Sin tipo de cambio activo | El interruptor explica que no hay conversión posible, en vez de quedarse sin efecto |
| Licitación cerrada o vencida | Sus ofertas se muestran sin acciones: se conservan como evidencia (§8.9) |
| Transición de estado no permitida | Solo se ofrecen las válidas; si aun así se fuerza, el dominio la rechaza y se muestra el motivo |
| Eliminación | Siempre pide confirmación, enlazada por delegación para que funcione en contenido añadido después de cargar |
| Monto con céntimos | Se acepta tanto `999999.99` como `999999,99` |

## Dos fallos que solo aparecieron al ejecutar

Ambos compilaban sin una sola advertencia y se detectaron al usar la interfaz
contra PostgreSQL real. Quedan anotados porque son el tipo de error que una
prueba unitaria no habría encontrado.

**Fechas con desplazamiento distinto de cero.** Npgsql solo admite
`DateTimeOffset` con desplazamiento UTC al escribir en `timestamp with time
zone`. El formulario construía la fecha con `-06:00` y la inserción fallaba con
`DbUpdateException`. Se interpreta la fecha en hora de Costa Rica y se convierte
a UTC, que es además lo que pide el §8.2.

**Decimales y cultura.** Un `<input type="number">` envía siempre el valor en
formato invariante, con punto, sea cual sea el idioma del navegador. Como la
aplicación usa cultura `es-CR`, el enlazador del marco rechazaba `999999.99` con
«no es válido» y **ninguna persona podía registrar un monto con céntimos**.
`EnlazadorDecimal` intenta primero el formato invariante y recurre a la cultura
de la petición para los campos de texto.

El mismo desajuste afectaba a `data-monto-crc`: al renderizarse con `es-CR` salía
`999999,99` y `parseFloat` cortaba en la coma, perdiendo los céntimos en la vista
en dólares. Ahora el atributo se escribe con cultura invariante.

## Accesibilidad

- Menú colapsable con `aria-label` y `aria-expanded`.
- Interruptores de tema y moneda con `aria-pressed`.
- Mensajes con `role="alert"` o `role="status"` para que se anuncien.
- Tablas con `<caption>` oculto visualmente y `scope` en las cabeceras.
- Foco siempre visible mediante `:focus-visible`.

## Pruebas

Los listados y formularios se verificaron ejecutando el flujo del §5.3 contra la
aplicación real: alta de proveedor, duplicado normalizado rechazado, creación y
publicación de licitación, oferta válida, oferta duplicada rechazada, oferta
superior al presupuesto rechazada, y mejor oferta con su clasificación y
aprobador. Las pruebas de navegador automatizadas llegan con HU-17.
