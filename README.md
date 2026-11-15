# Scape From the Heatwave

Videojuego de plataformas 2D hecho en Unity para el primer reto de **PMD (Programación Multimedia y Dispositivos Móviles)**, 2º de Desarrollo de Aplicaciones Multiplataforma.

El juego trata el **ODS 13: Acción por el clima**. Verano de 2050: Marta queda atrapada en la ciudad en plena ola de calor y tiene que llegar a un **refugio climático** antes de sufrir un golpe de calor. Para conseguirlo tiene que buscar la sombra, beber agua y usar objetos que la protejan del sol.

## Equipo

| Integrante | Responsabilidad |
|---|---|
| Christian Garay | Personaje (movimiento y Animator), GameManager, plataformas móviles |
| Álvaro | Interfaz (menús, HUD, pausa), sonido y música |
| Anartz | Diseño de niveles y objetos recogibles |

## Tecnología

- **Unity 6000.0.30f1**, plantilla 2D con **URP**
- **Input System** nuevo, con las acciones definidas en `Assets/InputSystem_Actions.inputactions`
- **Cinemachine** para que la cámara siga al personaje
- Plataformas objetivo: **PC** y **Android**

## Cómo abrir el proyecto

1. Clonar el repositorio.
2. En Unity Hub, *Add → Add project from disk* y seleccionar la carpeta `ScapefromtheHeatwave/`, no la raíz del repositorio.
3. Abrir con Unity **6000.0.30f1**.
4. Escenas en `Assets/Scenes/`:
   - `MenuPrincipal`: menú de inicio.
   - `Nivel1`: el nivel completo.
   - `PruebaPersonaje`: escena de pruebas con el personaje y el HUD.

## Controles (PC)

| Acción | Tecla |
|---|---|
| Moverse | A / D o flechas |
| Saltar | Espacio (mantener para saltar más alto) |
| Correr | Shift izquierdo |
| Usar objeto | E |
| Pausa | Esc |

## El nivel

`Nivel1` recorre cuatro zonas de la ciudad, cada una con sus propios tiles y fondo con efecto *parallax*:

1. **Barrio residencial**
2. **Avenida principal**: mucho sol y pocas sombras.
3. **Zona industrial**: chorros de aire caliente.
4. **Parque y refugio climático**: la meta.

## Mecánicas

### Calor
- Sube solo, poco a poco. Lleno del todo es un golpe de calor.
- En la **sombra** se frena. Algunas sombras (farolas, palmeras, árbol grande) además lo hacen **bajar**.
- La **gorra** reduce el calor a la mitad durante 15 segundos.
- Los **chorros de aire caliente** de la zona industrial se activan cada pocos segundos y suben el calor de golpe.
- Si llega al 100 %, Marta **reaparece en el último checkpoint** con el calor al 50 %.
- La pantalla se tiñe de naranja y, por encima del 70 %, de rojo con un pulso.

### Hidratación
- Se gasta al **correr**, y también sola cuando el calor pasa del 70 %.
- Solo se recupera **bebiendo**: en las **fuentes** o con una **botella de agua**.
- Si baja del 20 %, el calor sube un 20 % más rápido.
- Si llega a 0, Marta **no puede correr**.

### Objetos
- Marta tiene **un solo hueco** de inventario, que se ve arriba a la derecha.
- Si el hueco está ocupado, no puede recoger otro objeto.
- **Botella**: recupera un 30 % de hidratación.
- **Gorra**: protege del calor durante 15 s.

### Recorrido
- **Checkpoints** repartidos por el nivel.
- Si Marta cae al vacío, reaparece en el último checkpoint con un 10 % más de calor.
- **Plataformas móviles** que llevan a Marta en horizontal, vertical o diagonal.
- Al llegar a la **meta** se vuelve al menú.

### Sonido y efectos
- Música de fondo que suena "como una radio" en la sombra y se distorsiona con el calor alto.
- Efectos de pasos, salto, aterrizaje, caída, sprint, beber, recoger, gorra, checkpoint, pausa y botones.
- Al correr, la cámara se aleja y aparecen líneas de viento.

## Estructura del proyecto

```
ScapefromtheHeatwave/Assets/
├── Personaje/
│   ├── Scripts/        Controles, EstadoJugador, Inventario, SonidosPersonaje,
│   │                   CamaraAdrenalina, EfectoViento
│   └── Sprites/        Sprites y Animator de Marta (Idle, Run, Jump)
├── Nivel/
│   ├── Scripts/        Zonas, objetos, checkpoints, música y efectos del nivel
│   ├── Sprites/        Árboles, coches, farolas, fuente, botella, gorra…
│   └── Tiles/          Tiles de cada zona
├── Juego/Scripts/      GameManager
├── Entorno/Scripts/    PlataformaMovil
├── UI/Scripts/         HUDController, BarraUI, MenuPausa, MenuPrincipal
├── Resources/          Efectos de sonido (se cargan por nombre con SFX.Play)
├── Editor/             Herramientas para generar el nivel y la UI
├── Prefabs/            Personaje, GameManager, UI_Juego, Plataforma
└── Scenes/             MenuPrincipal, Nivel1, PruebaPersonaje
```

## Scripts principales

| Script | Qué hace |
|---|---|
| `Controles` | Movimiento de Marta: andar, correr, saltar (con *coyote time*) y parámetros del Animator. |
| `EstadoJugador` | Calor e hidratación (de 0 a 1) y sus reglas. Actualiza el HUD. |
| `Inventario` / `Recogible` | Recoger y usar la gorra o la botella. |
| `ZonaSombra` | Sombra: frena el calor o, con `reduceCalor`, lo hace bajar. |
| `Fuente` | Recupera hidratación mientras Marta está dentro. |
| `AireCaliente` | Chorro de calor que se activa cada cierto tiempo. |
| `Checkpoint` / `ZonaMuerte` | Puntos de reaparición y caída al vacío. |
| `Meta` | Final del nivel. |
| `SFX` / `MusicaNivel` / `SonidosPersonaje` | Efectos de sonido, música dinámica y sonidos de Marta. |
| `FiltroCalor`, `CamaraAdrenalina`, `EfectoViento`, `FondoBiomas` | Efectos visuales y fondo. |
| `PlataformaMovil` | Plataforma que va y viene. Se configura con el vector `recorrido`, por ejemplo `(3, 0)` para horizontal. |
| `GameManager` | Cuenta atrás y paneles de victoria y derrota. Se accede con `GameManager.instancia`. |
| `HUDController` | Actualiza las barras del HUD y el icono del objeto. |
| `MenuPausa` / `MenuPrincipal` | Menús del juego. |

## Cómo montar elementos del nivel

Cada elemento necesita un `Collider2D` con **Is Trigger** y el script correspondiente:

| Elemento | Script | Ajustes |
|---|---|---|
| Sombra | `ZonaSombra` | `reduceCalor`: si está marcado, el calor baja; si no, solo deja de subir |
| Fuente | `Fuente` | `hidratacionPorSegundo` |
| Objeto | `Recogible` | `tipo`: Gorra o Botella |
| Checkpoint | `Checkpoint` | `puntoReaparicion`: dónde reaparece Marta |
| Caída al vacío | `ZonaMuerte` | Un trigger largo debajo de todo el nivel |
| Aire caliente | `AireCaliente` | `periodo`, `duracionActiva`, `calorPorSegundo` |
| Meta | `Meta` | `escenaSiguiente` |

**Plataforma móvil:** usar el prefab `Plataforma`, o un objeto con sprite, `BoxCollider2D` y el script `PlataformaMovil`. El script añade el `Rigidbody2D` por sí mismo. Hay que dejar **desmarcadas las Constraints** del Rigidbody2D. En la vista Scene, una línea amarilla muestra el recorrido.

⚠️ **Herramienta *Tools → Scape From the Heatwave → Crear Nivel Completo*:** genera `Nivel1` desde cero a partir de `PruebaPersonaje`. **Borra todos los cambios hechos a mano en `Nivel1`.** No usarla si ya se ha modificado el nivel.

## Pendiente

- [ ] El botón **JUGAR** del menú carga `PruebaPersonaje` en vez de `Nivel1`.
- [ ] Botón y panel de **Créditos** en el menú principal.
- [ ] **Pantalla de victoria o derrota**. El `GameManager` ya tiene `Ganar()` y `Perder()`, pero no está en `Nivel1`.
- [ ] Usar **prefabs** para los objetos del nivel y colocar plataformas móviles en `Nivel1`.
- [ ] Añadir de nuevo el estado **Dano** al Animator.
- [ ] **Build de Android**.

## Flujo de trabajo con Git

- `main`: versión estable para entregar.
- `dev`: rama de integración, donde se juntan los trabajos.
- Cada integrante trabaja en su propia rama y la une a `dev` con un *Pull Request*.

No se deben subir las carpetas `Library/`, `Logs/`, `Temp/` ni `UserSettings/`, porque Unity las genera solo. El `.gitignore` las excluye **solo dentro de `ScapefromtheHeatwave/`**. Por eso hay que abrir siempre esa carpeta en Unity Hub y no la raíz del repositorio: si se abre la raíz, Unity crea esas carpetas fuera y acaban subiéndose.
