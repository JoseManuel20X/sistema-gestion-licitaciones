# Declaración de uso de herramientas de inteligencia artificial

Conforme a la sección 16 del enunciado, se declara el uso de herramientas de IA
como asistencia en este proyecto.

## Herramienta

- **Claude Code** (Anthropic), asistente de programación en línea de comandos.

## Finalidad y forma de uso

La IA se utiliza como **asistente bajo dirección del estudiante**, quien actúa
como cliente y programador del proceso XP: define prioridades, revisa cada
cambio, ejecuta las pruebas y valida los criterios de aceptación. Usos
concretos:

- Apoyo en la redacción inicial de la documentación de planificación XP (historias de usuario, plan de iteraciones), revisada y ajustada por el estudiante.
- Asistencia en la configuración del entorno (estructura de solución, Docker, CI).
- Discusión de casos de prueba y de alternativas de implementación durante los ciclos TDD, siempre con la decisión final tomada por el estudiante.
- Revisión de código y sugerencias de refactorización.
- Consulta de dudas técnicas sobre .NET 9, EF Core, PostgreSQL, Docker y Kubernetes.

## Módulos asistidos

Esta sección se actualiza en cada iteración indicando en qué módulos y con qué
alcance intervino la asistencia de IA.

| Iteración | Módulo | Alcance de la asistencia | Validación realizada por el estudiante |
|---|---|---|---|
| Planning | Documentación XP inicial | Borrador de historias, plan y estructura de docs a partir del enunciado. | Lectura completa, ajuste de prioridades y estimaciones en el Planning Game. |
| 1 | Configuración de calidad | `Directory.Build.props`, `.editorconfig`, supresiones de reglas de análisis. | Revisión de cada supresión y de su justificación escrita; verificación de que la compilación falla ante advertencias. |
| 1 | Dominio: proveedores y licitaciones | Entidades, normalización Unicode, ciclo de estados, `IReloj`, política de redondeo. | Ejecución de las pruebas, revisión regla por regla contra §8 del enunciado. |
| 1 | Persistencia | `DbContext`, configuraciones, migración inicial, datos semilla. | Inspección del SQL generado: tipos `numeric(18,2)`, índices únicos parciales, CHECK y `xmin`. |
| 2 | Dominio: ofertas, mejor oferta, niveles | `EvaluadorOfertas`, `TablaNivelesAprobacion`, reglas de vencimiento y duplicidad. | Verificación de los casos límite (umbral del 10 %, empate, instante exacto del cierre) contra el enunciado. |
| 2 | Aplicación | Casos de uso, `Resultado<T>`, traducción de errores, paginación. | Revisión de la correspondencia entre `TipoError` y códigos HTTP; comprobación de que ningún fallo escapa sin tratar. |
| 2 | Pruebas | Pruebas unitarias y de integración con Testcontainers. | Ejecución completa, revisión de escenarios y medición de cobertura. |

## Validaciones realizadas

- Toda contribución asistida se revisa antes de integrarse; ninguna se acepta sin comprender su funcionamiento.
- Las reglas de negocio se verifican con pruebas automatizadas escritas siguiendo TDD.
- El estudiante puede explicar y modificar cualquier parte del sistema, como exige la defensa oral.
- Cada decisión de diseño no evidente está documentada con su porqué y sus alternativas descartadas, en [arquitectura-general.md](arquitectura-general.md) y en la [bitácora](bitacora-xp.md), de modo que pueda sostenerse en la defensa sin recurrir a «la IA lo generó».

## Decisiones que el estudiante debe poder defender

Lista de repaso para la defensa oral. Cada punto está desarrollado en la
documentación enlazada:

1. Por qué monolito modular y no microservicios ([arquitectura-general.md](arquitectura-general.md)).
2. Por qué las entidades lanzan excepción y los casos de uso devuelven `Resultado<T>`.
3. Por qué `Estado` y `EstadoEfectivo` están separados ([modulos/licitaciones.md](modulos/licitaciones.md)).
4. Por qué se usa `xmin` en vez de una columna de versión propia ([modelo-datos.md](modelo-datos.md)).
5. Por qué los índices únicos son parciales (`WHERE DeletedAt IS NULL`).
6. Por qué se eliminaron las colecciones de navegación `Ofertas` ([bitacora-xp.md](bitacora-xp.md), iteración 2).
7. Por qué la normalización **no** elimina diacríticos ([modulos/proveedores.md](modulos/proveedores.md)).
8. Por qué el reloj se inyecta y qué prueba sería imposible sin ello.
9. Por qué el redondeo es `AwayFromZero` y está centralizado en `Dinero`.
10. Por qué la unicidad se valida tres veces y qué pasa en una carrera entre dos peticiones.

## Límites

- La IA no constituye un integrante adicional del equipo ni sustituye la responsabilidad del estudiante.
- No se insertan comentarios artificiales, mensajes ocultos ni contenido ajeno a la funcionalidad.
