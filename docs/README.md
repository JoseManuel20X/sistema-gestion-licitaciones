# Documentación del Sistema de Gestión de Licitaciones

Proyecto final del curso ITI-822 Metodologías Ágiles de Desarrollo de Software (UTN),
desarrollado con Extreme Programming (XP) como única metodología.

## Índice de navegación

### Proceso XP

| Documento | Contenido | Estado |
|---|---|---|
| [Visión y alcance](vision-alcance.md) | Propósito del sistema, alcance funcional y restricciones. | ✅ |
| [Historias de usuario](historias-usuario.md) | Historias con prioridad, estimación y criterios de aceptación. | ✅ |
| [Plan XP](plan-xp.md) | Plan de liberación, iteraciones y reglas de trabajo. | ✅ |
| [Bitácora XP](bitacora-xp.md) | Resultados por iteración: velocidad, TDD, refactorizaciones y retroalimentación. | ✅ Iteraciones 1-2 |
| [Uso de IA](uso-ia.md) | Declaración de uso responsable de herramientas de inteligencia artificial. | ✅ |
| [Plan de iteraciones 3 y 4](plan-iteraciones-3-4.md) | Detalle técnico de las iteraciones pendientes. | ✅ |

### Diseño y arquitectura

| Documento | Contenido | Estado |
|---|---|---|
| [Arquitectura general](arquitectura-general.md) | Capas, dependencias y decisiones de diseño. | ✅ |
| [Modelo de datos](modelo-datos.md) | Entidades, relaciones, restricciones e índices. | ✅ |
| [Integración de módulos](integracion-modulos.md) | Cooperación entre módulos y flujos de extremo a extremo. | ✅ Iteraciones 1-2 |

### Módulos

| Documento | Contenido | Estado |
|---|---|---|
| [Proveedores](modulos/proveedores.md) | Normalización de nombres y unicidad. | ✅ |
| [Licitaciones](modulos/licitaciones.md) | Ciclo de estados, unicidad de código y reglas de presupuesto. | ✅ |
| [Ofertas](modulos/ofertas.md) | Validaciones, mejor oferta y clasificación de ahorro. | ✅ |
| [Niveles de aprobación](modulos/niveles-aprobacion.md) | Rangos parametrizables sin traslape. | ✅ |
| [Persistencia](modulos/persistencia.md) | EF Core, migraciones, auditoría y concurrencia. | ✅ |
| [Tipo de cambio](modulos/tipo-cambio.md) | Administración del tipo de cambio y conversión CRC/USD. | ✅ |
| [Interfaz web](modulos/interfaz-web.md) | Navegación, temas, formularios y accesibilidad. | ✅ |
| [API REST](modulos/api-rest.md) | Contratos HTTP, versionado y manejo de errores. | ✅ |

### Operación

| Documento | Contenido | Estado |
|---|---|---|
| [Pruebas](pruebas.md) | Estrategia de pruebas, ejecución y cobertura. | ✅ |
| [API](api.md) | Endpoints, ejemplos de solicitud/respuesta y errores. | ✅ |
| Docker | Construcción y ejecución con Docker Compose. | ⏳ Iteración 4 |
| Kubernetes | Despliegue, probes y persistencia en Kubernetes. | ⏳ Iteración 4 |

> Los documentos se crean y actualizan en la iteración en la que su funcionalidad
> se implementa, siguiendo la práctica XP de documentar lo que existe y no diseño
> especulativo. Los enlaces se activan al crearse el archivo, para que el índice
> nunca apunte a documentos inexistentes.

## Estado de verificación

| Comprobación | Resultado |
|---|---|
| `dotnet build` | ✅ 0 errores, 0 advertencias |
| `dotnet test` (unitarias) | ✅ 183 pruebas |
| `dotnet test` (integración, PostgreSQL 16 real) | ✅ 13 pruebas |
| Cobertura `Domain` | ✅ 94,7 % (mínimo 80 %) |
| Cobertura `Application` | ✅ 84,3 % (mínimo 80 %) |
