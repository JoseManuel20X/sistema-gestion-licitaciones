# Documentación del Sistema de Gestión de Licitaciones

Proyecto final del curso ITI-822 Metodologías Ágiles de Desarrollo de Software (UTN),
desarrollado con Extreme Programming (XP) como única metodología.

## Índice de navegación

### Proceso XP

| Documento | Contenido |
|---|---|
| [Visión y alcance](vision-alcance.md) | Propósito del sistema, alcance funcional y restricciones. |
| [Historias de usuario](historias-usuario.md) | Historias con prioridad, estimación y criterios de aceptación. |
| [Plan XP](plan-xp.md) | Plan de liberación, iteraciones y reglas de trabajo. |
| [Bitácora XP](bitacora-xp.md) | Resultados por iteración: velocidad, TDD, refactorizaciones y retroalimentación. |
| [Uso de IA](uso-ia.md) | Declaración de uso responsable de herramientas de inteligencia artificial. |

### Diseño y arquitectura

| Documento | Contenido |
|---|---|
| [Arquitectura general](arquitectura-general.md) | Capas, dependencias y decisiones de diseño. |
| [Modelo de datos](modelo-datos.md) | Entidades, relaciones, restricciones e índices. |
| [Integración de módulos](integracion-modulos.md) | Cooperación entre módulos y flujos de extremo a extremo. |

### Módulos

| Documento | Contenido |
|---|---|
| [Licitaciones](modulos/licitaciones.md) | Ciclo de estados, unicidad de código y reglas de presupuesto. |
| [Proveedores](modulos/proveedores.md) | Normalización de nombres y unicidad. |
| [Ofertas](modulos/ofertas.md) | Validaciones, mejor oferta y clasificación de ahorro. |
| [Niveles de aprobación](modulos/niveles-aprobacion.md) | Rangos parametrizables sin traslape. |
| [Tipo de cambio](modulos/tipo-cambio.md) | Administración del tipo de cambio y conversión CRC/USD. |
| [Interfaz web](modulos/interfaz-web.md) | Navegación, temas, formularios y accesibilidad. |
| [API REST](modulos/api-rest.md) | Contratos HTTP, versionado y manejo de errores. |
| [Persistencia](modulos/persistencia.md) | EF Core, migraciones, auditoría y concurrencia. |

### Operación

| Documento | Contenido |
|---|---|
| [API](api.md) | Endpoints, ejemplos de solicitud/respuesta y errores. |
| [Pruebas](pruebas.md) | Estrategia de pruebas, ejecución y cobertura. |
| [Docker](docker.md) | Construcción y ejecución con Docker Compose. |
| [Kubernetes](kubernetes.md) | Despliegue, probes y persistencia en Kubernetes. |

> Los documentos de módulos y operación se crean y actualizan en la iteración
> en la que su funcionalidad se implementa, siguiendo la práctica XP de
> documentar lo que existe y no diseño especulativo.
