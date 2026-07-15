# Plan XP

## Contexto del equipo

Proyecto en **modalidad individual**. Se aplican todas las prácticas XP
compatibles con esa modalidad: Planning Game, historias de usuario, iteraciones
cortas, liberaciones pequeñas, TDD, integración continua, diseño simple,
refactorización, estándares de código y ritmo sostenible. La programación en
parejas no aplica; en su lugar se realizan autorevisiones documentadas en la
bitácora y revisión asistida por herramienta de IA declarada en
[uso-ia.md](uso-ia.md).

## Roles

| Rol XP | Responsable |
|---|---|
| Cliente | Estudiante (prioriza historias y valida criterios de aceptación; el docente es el cliente final). |
| Programador | Estudiante. |

## Plan de liberación

Cuatro iteraciones de duración uniforme (una semana calendario cada una), cada
una con una liberación pequeña, ejecutable y demostrable, etiquetada en Git.

| Iteración | Tema | Historias | Puntos | Liberación |
|---|---|---|---|---|
| 1 | Fundación, proveedores y licitaciones | HU-01, HU-02, HU-03, HU-04 | 9 | `v0.1.0`: CRUD de proveedores y licitaciones con ciclo de estados, persistido en PostgreSQL. |
| 2 | Ofertas, mejor oferta y aprobación | HU-05, HU-06, HU-07, HU-08 | 10 | `v0.2.0`: flujo de ofertas completo con validaciones, mejor oferta y aprobador. |
| 3 | Moneda, interfaz y API | HU-09, HU-10, HU-11, HU-12, HU-13 | 9 | `v0.3.0`: experiencia web completa y API REST v1 documentada. |
| 4 | Despliegue, CI y E2E | HU-14, HU-15, HU-16, HU-17 | 10 | `v1.0.0` / `entrega-final`: solución contenerizada, desplegada y verificada E2E. |

La velocidad planificada es de 9-10 puntos por iteración; la velocidad
observada se registra al cierre de cada iteración en la
[bitácora XP](bitacora-xp.md) y ajusta el plan siguiente.

## Plan de iteración

Al inicio de cada iteración se realiza el Planning Game de iteración:

1. El cliente confirma o reordena las historias según prioridad.
2. Cada historia se divide en tareas técnicas pequeñas.
3. Cada tarea se implementa con TDD: prueba que falla → implementación mínima → refactorización.
4. Cada cierre de historia se integra con CI en verde y se anota en la bitácora.

## Reglas de trabajo XP

- **TDD**: ninguna regla de negocio se implementa sin una prueba escrita antes. Los commits reflejan el ciclo (por ejemplo `test:` seguido de `feat:` y `refactor:`).
- **Integración continua**: se integra al menos una vez por sesión de trabajo; no se deja la rama principal en rojo.
- **Diseño simple**: se implementa lo mínimo que satisface la historia vigente; no se agrega infraestructura especulativa.
- **Refactorización**: ante duplicación o código confuso se refactoriza en el momento, con las pruebas como red de seguridad.
- **Liberaciones pequeñas**: cada iteración termina con una versión etiquetada ejecutable con `docker compose up --build` (a partir de la iteración 4, también desplegable en Kubernetes).
- **Estándares de código**: convenciones de `.editorconfig`, análisis estático en CI, nombres descriptivos en español del dominio (entidades) e inglés técnico (infraestructura), documentación XML en API pública relevante.
- **Ritmo sostenible**: sesiones de trabajo distribuidas durante toda la iteración; el historial de commits debe reflejarlo.
- **Propiedad colectiva**: todo el código es de un único autor responsable que puede explicar y modificar cualquier módulo.

## Convenciones de commits

Se utiliza Conventional Commits con ámbito por módulo:

```
feat(ofertas): impedir registro después del vencimiento
fix(proveedores): normalizar espacios duplicados
test(api): cubrir conflicto por código repetido
refactor(aprobacion): simplificar búsqueda de rangos
docs(xp): registrar resultados de la iteración 3
```

Cada historia terminada se vincula con sus commits y pruebas en la bitácora.

## Definición de historia terminada

Una historia está terminada cuando:

1. Todos sus criterios de aceptación tienen prueba automatizada que los verifica.
2. La CI está en verde (compilación, pruebas, formato, análisis).
3. La funcionalidad es demostrable desde la interfaz o la API.
4. La documentación del módulo afectado en `/docs/modulos` está actualizada.
5. El cliente (estudiante, y en la defensa el docente) acepta los criterios.
