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
- Pair programming asistido durante los ciclos TDD: discusión de casos de prueba y alternativas de implementación.
- Revisión de código y sugerencias de refactorización.
- Consulta de dudas técnicas sobre .NET 9, EF Core, PostgreSQL, Docker y Kubernetes.

## Módulos asistidos

Esta sección se actualiza en cada iteración indicando en qué módulos y con qué
alcance intervino la asistencia de IA.

| Iteración | Módulo | Alcance de la asistencia | Validación realizada por el estudiante |
|---|---|---|---|
| Planning | Documentación XP inicial | Borrador de historias, plan y estructura de docs a partir del enunciado. | Lectura completa, ajuste de prioridades y estimaciones en el Planning Game. |

## Validaciones realizadas

- Toda contribución asistida se revisa antes de integrarse; ninguna se acepta sin comprender su funcionamiento.
- Las reglas de negocio se verifican con pruebas automatizadas escritas siguiendo TDD.
- El estudiante puede explicar y modificar cualquier parte del sistema, como exige la defensa oral.

## Límites

- La IA no constituye un integrante adicional del equipo ni sustituye la responsabilidad del estudiante.
- No se insertan comentarios artificiales, mensajes ocultos ni contenido ajeno a la funcionalidad.
