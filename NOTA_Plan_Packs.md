# NOTA — PLAN: PACKS INDIVIDUALISTAS POR JUGADOR

> ⚠️ **CORRECCION DEL USUARIO (2026-11-18) — DOS CASILLEROS, NO UNO:**
> El perfil es como un **casillero propio por jugador**: cada jugador
> guarda SU pack y SU color elegidos. El tablero es **neutral** (la
> cancha): ninguna casilla "pertenece" a un pack; cada casilla muestra
> el look del jugador que la tiene en ese momento (y cambia al ser
> conquistada). El juego debe tener **DOS casilleros** (P1 y P2), no
> uno solo: hay que probar las 4 combinaciones — P1 pack + P2 clasico,
> ambos clasico, P1 clasico + P2 pack, y cada uno con su pack.
> Esto CAMBIA el paso 3 (el pack del bot ya no es una constante: es el
> casillero del jugador 2) y el paso 4 (menu con DOS paneles).
>
> **ORDEN AJUSTADO (2026-11-18)**: el usuario quiere **ver el pack
> seleccionado y jugando COMPLETO y sin errores** antes que la tienda.
> Por eso el **Paso 5 (monedas/compra)** y el **Paso 14 (online)** se
> dejan para el FINAL, casi antes de ponerlo online. Prioridad ahora:
> 6 → 6b → 7 → 7b → 8-12 → 13 (render completo del caldero).
>
> **CHECKPOINT (Paso 0): commit `ac0cf39`** — "BASE SEGURA: menu caldero
> aprobado + juego original (pre-plan packs)". Antes de este commit, el
> ultimo era `3313402` ("ramking terminado"). Si algo se rompe:
> `git reset --hard ac0cf39` (con confirmacion del usuario).
>
> **Fecha: 2026-11-17.** Plan ordenado de FACIL → DIFICIL para el rediseño
> "individualista": cada jugador elige SU color y SU pack, y las casillas
> se renderizan segun el DUEÑO (sobrescritura al ser conquistadas).
>
> Esta vez SI se toca el codigo del juego completo — por eso el plan se
> hace en pasos chicos, cada uno con su prueba en Play y su punto de
> retroceso (lecciones 17 y del desastre del caldero, NOTA_Caldero.md
> seccion 8b-ter).

---

## 0. REGLAS DE TRABAJO (no negociables)

1. **UN paso a la vez.** Nunca dos pasos en la misma sesion sin cerrar el
   anterior. Si un paso se rompe → se revierte SOLO ese paso.
2. **Prueba en Play despues de CADA paso.** Criterio de aprobado escrito
   abajo; si no cumple, no se avanza.
3. **Guardar escenas SOLO fuera de Play** (regla de oro; los "cocidos"
   ya nos mordieron una vez).
4. **Checkpoint git antes de empezar** y un commit por cada paso
   aprobado. Asi revertir = volver al commit anterior, sin drama.
5. **Con "clasico" nada debe cambiar de vista.** Cada paso esta diseñado
   para que el look del clasico quede IDENTICO. Si el clasico cambia de
   aspecto, el paso esta mal.
6. Los valores de ajuste (tamaños, colores, knobs) viven en el pack o en
   el componente, y se calibran **en una celda del JUEGO**, nunca copiados
   del menu (el menu es canvas, el juego es world-space).

---

## 1. RESUMEN DE PASOS (facil → dificil)

| # | Paso | Dificultad | Toca el juego? |
|---|------|-----------|----------------|
| 0 | Checkpoint git + base verificada | — | No |
| 1 | `FxPack` gana color de liquido propio | ⭐ facil | Poco |
| 2 | `BoardFx.PackFor(owner)` (pack por dueño) | ⭐ facil | Poco |
| 3 | Dos casilleros (P1/P2) en el perfil + colores independientes | ⭐⭐ medio | Poco |
| 3b | Sonido y burbujas: sacar los fijos del juego y tomarlos del pack | ⭐⭐⭐ dificil | SI |
| 4 | Seleccionar el caldero desde el menu (clic equipar/quitar) | ⭐⭐ medio | No (solo menu) |
| 5 | Compra de packs con monedas/gemas | ⭐⭐ medio | No (solo menu) |
| 6 | Registrar el pack "caldero" en el catalogo (datos, sin render) | ⭐⭐ medio | Poco |
| 6b | Exportar la config visual del boton al pack (con conversion de escala) | ⭐⭐⭐ dificil | Poco |
| 7 | Render por casilla segun dueño (cristal + liquido + burbujas) | ⭐⭐⭐ dificil | SI |
| 7b | Marcador de dueño (● P1 / ○ P2) si color y pack coinciden | ⭐⭐ medio | SI |
| 8 | Objetos nivel 1 (hongos) en world-space, calibrados en celda de prueba | ⭐⭐⭐ dificil | SI |
| 9 | Objetos nivel 2 (ojo vivo + hongo grande) | ⭐⭐⭐⭐ dificil | SI |
| 10 | Objetos nivel 3 (hueso + segundo ojo) | ⭐⭐⭐⭐ dificil | SI |
| 11 | Vapor en world-space | ⭐⭐⭐⭐ dificil | SI |
| 12 | Boom con voladores (nivel 4) | ⭐⭐⭐⭐⭐ muy dificil | SI |
| 13 | Sobrescritura del pack al conquistar | ⭐⭐⭐ dificil | SI |
| 14 | PvP real + espejo por cliente (`viewerId`) | ⭐⭐⭐⭐⭐ muy dificil | SI |
| 15 | Marco personalizado por pack (hoy solo la tapa) | ⭐⭐⭐⭐ dificil | SI |
| 16 | OST por pack: cambia segun quien toma el control | ⭐⭐⭐⭐⭐ muy dificil | SI |

Regla de la casa: los pasos 1-6 son "seguros" (el juego queda jugable
aunque se corte ahi). Desde el 7 en adelante se entra en render real —
a partir de ahi, si algo se rompe, se revierte al paso anterior sin
pena y se reintenta con celdas de prueba.

---

## 2. DETALLE DE CADA PASO

### Paso 0 — Checkpoint (el usuario)
- [x] Commit git del proyecto entero con el juego funcionando como hoy
      (BASE SEGURA post-emergencia) → **`ac0cf39`**.
- [ ] Verificado: Play en GameScene = casillas de cristal con liquido
      animado, sin errores en consola (lo confirma el usuario).

### Paso 1 — Color de liquido propio del pack ⭐ (IMPLEMENTADO)
- **Que**: `FxPack` gana `usaColorPropio` (bool) + `colorLiquidoPropio`
  (Color). `BoardFx.LiquidColorFor(owner, pack)` nuevo; `CellVisuals`
  tintea con el color del PACK en vez del global por dueño.
- **Archivos tocados**: `FxPacks.cs` (+2 campos), `BoardFx.cs`
  (+1 sobrecarga), `CellVisuals.cs` (3 puntos: liquido ~624, foam ~644,
  boom ~800). `GameManager` NO se toco (sigue con la sobrecarga vieja).
- **Prueba en Play**: con "clasico" se ve IDENTICO a hoy (rosa/naranja).
- [ ] Aprobado por el usuario (falla si algo cambio de vista o hay error)

### Paso 2 — Pack por dueño (ruteo) ⭐ (IMPLEMENTADO)
- **Que**: `BoardFx.PackFor(owner)`: 1 = pack del perfil (nube), 2 =
  `BotPackId`, resto = clasico. `Current` queda como alias de `PackFor(1)`
  (compatibilidad). `CellVisuals` pregunta el pack POR CASILLA (ya conoce
  `owner`): color de liquido/foam/boom y clips de nivel/boom.
- **Archivos tocados**: `BoardFx.cs`, `CellVisuals.cs`, `GameManager.cs`.
- **Seguridad**: `PackDeCelda` cae al pack del Inspector si el pack del
  dueño no trae arte (asi un pack de solo color no rompe animaciones); el
  COLOR usa el pack del dueño aunque no tenga arte (para el paso 6).
- **Prueba en Play**: ambos jugadores = "clasico" → IDENTICO a hoy.
- [ ] Aprobado por el usuario (falla si algo cambio de vista o hay error)

### Paso 3 — Segundo CASILLERO (datos del jugador 2) ⭐⭐ (IMPLEMENTADO)
- ~~Antes: `BotPackId = "clasico"` constante.~~ **Correccion del usuario**:
  el jugador 2 tiene SU PROPIO casillero, con pack y color elegidos.
- **Que hecho**: `PlayerProfile` gana `EquippedPackId2`, `PlayerColorMode2`
  (default naranja = look actual), `PlayerCustomColor2`, `OwnedPackIds2`.
  Cloud Save gana las claves `EquippedPack2/PlayerColor2/PlayerCustomColor2/
  OwnedPacks2`. `BoardFx.PackFor(2)` lee ese pack (constante `BotPackId`
  queda SOLO como fallback). `ApplyColors` ahora recibe los DOS colores y
  son INDEPENDIENTES (ya no se deriva P2 desde P1). La API de perfil
  (`EquipPack`, `UnlockPack`, `SetColorMode`, `SetCustomColor`,
  `OwnsPack`) acepta `jugador = 1|2` con default 1 → las llamadas viejas
  del menu siguen funcionando. `FxPack` gana `sonidoBoom` (las burbujas
  ya estaban en `bubbleSprites`).
- **Archivos**: `UGSProfileManager.cs`, `BoardFx.cs`, `FxPacks.cs`,
  `CellVisuals.cs` (AttachArt pasa el sonido inicial al pack).
- **CORRECCION tras la prueba del usuario**: al elegir "Naranja" para P1,
  P2 tambien quedaba naranja (ambos iguales). El casillero 2 ahora nace
  en modo AUTOMATICO (`PlayerColorMode2 = -1`): hasta que se le elija un
  color explicitamente, se diferencia solo del P1 (rosa <-> naranja, o
  HSV opuesto con la rueda). Al elegir color en el paso 4, queda fijo.
  Se agrega `ColorJugador2Elegido` (clave `Color2Elegido`) para que un
  valor `PlayerColor2=1` guardado por la prueba anterior NO cuente como
  eleccion: mientras la marca sea false, el modo automatico manda.
- **Prueba**: ambos casilleros en clasico = identico a hoy; cambiar el
  pack/color de P2 affecta SOLO a sus casillas.
- [ ] Aprobado (PENDIENTE de prueba en Play; el paso 2 tambien sigue sin
      approbar — probar juntos y commitear)

### Paso 3b — Migrar SONIDO y BURBUJAS al pack ⭐⭐⭐ (IMPLEMENTADO)
- **Que**: `FxPack` gana todos los valores de burbujas (`burbujasOn`,
  `bubbleRate`, `bubbleSize`, `bubbleRise`, `bubbleDrift`, `bubbleAlpha`,
  `bubbleOrder` + `bubbleSprites` ya existia) y de sonido (`sonidoBoom`,
  `sonidoBoomB`, `modoSonidoBoom`, `boomVolume`). `CellVisuals` YA NO lee
  sus campos viejos en tiempo de juego: `SetupBubbles`, `UpdateBubbleZone`,
  `SetBubbleRate`, el tinte de foam y `PlayBoomSound` consultan
  `PackDeCelda(owner)` / `colorPack` / `sPack`.
- **NO hay switch global** (correccion final del usuario): se elimino el
  bool `bubbles` del Inspector. Cada pack decide: `burbujasOn = false` en
  el **clasico** (no se ven); `true` en un pack especial (el **caldero**
  traera las suyas, generadas por codigo como en el menu, en el paso 7).
  Con P1=caldero y P2=clasico, las burbujas SOLO se ven en las casillas
  de P1. Lo que uno ponga en su casillero es lo que se ve.
- **PUENTE**: los campos viejos del Inspector siguen existiendo SOLO como
  fuente de migracion del pack "clasico" (sonido, sprites y ajustes);
  `MigrarEfectosAlPack()` los copia una vez en Awake. Los datos
  UNIVERSALES de la cancha (superficie por nivel, layout, ordenes de
  capa del tablero) quedan fuera del pack.
- **Archivos**: `FxPacks.cs`, `CellVisuals.cs`.
- **Prueba en Play**: burbujas y sonido del boom IGUALES a hoy; sin
  errores en consola.
- [x] Aprobado por el usuario (commit `1913546`)

### Paso 4 — Seleccionar el caldero desde el MENU ⭐⭐ (IMPLEMENTADO)
- **Correccion del usuario**: en online NO hay dos paneles en una misma
  pantalla: cada jugador usa el MISMO menu en SU dispositivo y escribe
  SU perfil en la nube (por PlayerId). Los datos de los dos casilleros
  ya estan hechos (paso 3); lo que faltaba era el BOTON que permita
  equipar/guardar el pack.
- **Que hecho**: `SwatchCaldero.SeleccionarPack()` vuelve (toggle): clic
  desbloquea + equipa + guarda en nube/local; clic otra vez vuelve al
  "clasico". El boton se sostiene 6% mas grande mientras su pack esta
  equipado. `packIdSeleccion` / `seleccionarAlClic` son campos nuevos
  (toman defaults del codigo).
- **Registro de datos**: el boton registra `new FxPack("caldero", ...)`
  en el catalogo (SIN arte) para que `EquipPackAsync` lo acepte. El
  juego NO se rompe: `CellVisuals.Update` ignora packs sin `HasArt`
  (salvaguarda), asi que sigue dibujando el clasico hasta el paso 7.
- **GRUPO de seleccion unico** (pidio el usuario): al elegir un color,
  `ColorSwatch` equipa el pack "clasico" (`equiparClasicoAlClic`) y su
  boton se agranda; si despues se elige el caldero, todos los colores
  vuelven a escala normal. La regla vive en el perfil: un color solo se
  ve seleccionado si su color coincide Y `EquippedPackId == "clasico"`.
  `ColorSwatch` NO menciona el caldero en ningun lado (cero rastros en
  scripts compartidos); solo conoce el pack basico.
- **Ajuste visual del usuario**: el caldero era 1.06 (6%) y se veia chico;
  ahora `escalaSeleccion = 1.25` (25%, mas resalte, pack premium) y los
  colores `1.18`. El hover usa `Mathf.Max(hoverScale, escalaSeleccion)`
  para que un boton seleccionado NUNCA se encoja al pasar el mouse.
- **Logs de consola** (pidio el usuario, para verificar la caja elegida):
  al abrir el panel `[Selector] Caja actual del jugador: '...'`; al elegir
  caldero `[Selector] ✅ Pack 'caldero' EQUIPADO y GUARDADO ... LISTO PARA
  JUGAR CON CALDERO (el look se aplica en el paso 7)`; al elegir color
  `[Selector] ✅ Elegiste color ... Caja guardada: 'clasico' → LISTO PARA
  JUGAR CLASICO`. `ColorSwatch` no nombra el caldero en el log (dice
  "pack especial"). Importante: la consola confirma la ELECCION guardada;
  el tablero sigue viendo clásico hasta que el pack tenga arte (paso 7).
- **Prueba**: consola muestra EQUIPADO / QUITADO; el perfil persiste al
  reabrir; el juego sigue con el clasico.
- [ ] Aprobado por el usuario (PENDIENTE de probar)

### Paso 5 — Compra de packs ⭐⭐
- **Que**: comprar con `SpendCurrencyAsync` (existe) antes de
  desbloquear. Precios por pack (tabla en esta nota o Remote Config).
- **Prueba**: comprar, quedar `OwnedPacks` actualizado, monedas bajan,
  persiste en nube.
- [ ] Aprobado

### Paso 6 — Registrar "caldero" (solo datos) ⭐⭐ (IMPLEMENTADO)
- **Que hecho**: el caldero ya vive en el catalogo de `FxPacks.cs` (no
  depende del boton del menu, asi funciona abriendo GameScene directo).
  Receta: `usaColorPropio = true` + verde `0.15/0.65/0.25` +
  `burbujasOn = true`. **Sin clips/prefab** (`HasArt = false`): el
  cristal y las animaciones siguen siendo los del clasico (paso 7).
- **Sonido propio nuevo** (lo agrego el usuario): `Assets/musica y
  sonidos/explosion_caldero.mp3`. Se carga por nombre desde el catalogo
  (editor) y se pone en la ranura A del pack caldero con modo "solo A";
  el clasico conserva su sonido A/B del Inspector. Si queda para build,
  mover el mp3 a `Assets/Resources` o asignarlo en el Inspector.
- `CellVisuals` separo `PackEfectos(owner)` (burbujas/sonido/color, que
  NO necesitan arte) de `PackDeCelda(owner)` (clips, que si lo necesitan).
  El caldero hereda el sonido del clasico mientras no tenga el suyo.
- **Resultado visible ya**: si la caja guardada es "caldero", las
  casillas del jugador 1 salen **VERDES con burbujas**; las del jugador
  2 siguen clasico (rosa/naranja). Sin errores en consola.
- **CORRECCION tras la prueba del usuario** (veia el color pero no las
  burbujas): el tablero nace neutral (`owner = 0`), asi que al crear la
  celda se consultaba el pack del tablero (clasico, sin burbujas) y el
  emisor ni se creaba. Ahora el emisor se crea SIEMPRE (callado) y
  `ApplyState` lo reconfigura en vivo cuando cambia el pack del dueno
  (`CellVisual.bubblePackId`); `SetBubbleRate` calla las del pack que no
  trae burbujas. **El vapor todavia no existe en el juego** (es el paso
  11: era solo del menu).
- **Correcciones tras la prueba del usuario** (2026-11-18):
  1. Error `Setting the duration while system is still playing`: al
     cambiar de dueno (una explosion del enemigo) se reconfiguraba el
     emisor con particulas vivas. `SetupBubbles` ahora llama
     `ps.Stop(true, StopEmittingAndClear)` ANTES de tocar `duration`.
  2. Burbujas "poquitas": densidad 5/s, tamano 0.09 y `startColor` con
     `MinMaxGradient` (unas tenues, otras marcadas, como el menu).
  3. "Primero aparecio otro color": `UGSProfileManager.PerfilListo`
     (true al terminar `LoadProfileAsync`) evita seleccion provisional
     antes de cargar la nube; `OnProfileLoaded` refresca el boton.
  4. Volumen del boom: despues de bajar el caldero, el usuario escucho
     BAJO el clasico. Ahora ambos son ajustables en el Inspector de
     `CellVisuals`: `volumenBoomClasico = 1.3` (0.8 → 1.0) y
     `volumenBoomCaldero = 0.6` (0.8 → 0.48). Campos NUEVOS: toman el
     default del codigo aunque la escena tenga valores viejos; se pueden
     mover en Play hasta que suenen igual.
  5. Prueba MANUAL de sonidos (pidio el usuario): `CellVisuals` gana
    `calderoBoomSoundA/B` + `calderoBoomSound` (A/B/Alternar). Si se
    arrastra un clip ahi, MANDA sobre el mp3 automatico; si se deja
    vacio, se usa `explosion_caldero.mp3`. El clasico se cambia con
    `boomSoundA/B` de siempre. `MigrarEfectosAlPack()` corre tambien en
    Update: los cambios en Play se aplican en la proxima explosion.
- **Archivos**: un loader chico en `Assets/scrips/packs/`.
- **Prueba**: equipar caldero en el menu → GameScene → casillas verdes
  con burbujas solo en P1; sin errores en consola.
- [ ] Aprobado por el usuario (PENDIENTE de probar)

### Paso 6b — Exportar la config VISUAL del botón al pack ⭐⭐⭐ (NUEVO)
- **Idea del usuario**: el boton del caldero ya guarda (en la escena del
  menu) tamanos, colores, posiciones, burbujas, vapor y animaciones de
  liquidos — es un "guardado visual". ¿Por que no subirlo a la nube?
- **Respuesta/disefio**: los datos se LEEN del boton (`ExportarConfigCaldero()`
  leyendo `SwatchCaldero` + `ObjetoEnLiquidoUI`/`OjoFlotanteUI`/
  `BurbujasCalderoUI`/`VaporCalderoUI`) pero se guardan como **RECETA del
  pack** (en `FxPack`/asset), NO en la nube:
  - la nube guarda solo la **eleccion** (`EquippedPackId`) → liviana y
    a prueba de desactualizaciones;
  - hace falta **conversion canvas → mundo** (el boton es UI, la celda
    mide 1.2u) para que no salgan deformes;
  - los clips de animacion del juego viven en `CellVisuals` (los frames
    del boton son solo del liquido UI); cada pack traera los suyos.
- **Resultado**: lo calibrado a ojo en el menu se convierte en la config
  del pack sin copiar valores a mano.
- [ ] Pendiente (despues del paso 6, antes del render del paso 7)

### Paso 7 — Render por casilla ⭐⭐⭐
- **Que**: `CellVisuals` arma cada casilla con el pack de SU dueño:
  cristal, clips y color del pack. Casilla de P1 con caldero = liquido
  verde; casilla del bot = clasico normal.
- **Archivos**: `CellVisuals.cs` (el gordo), `BoardFx.cs`.
- **Prueba**: P1 = caldero, bot = clasico → tus casillas verdes, las
  del bot rosa/naranja como siempre. Con ambos clasico = IDENTICO.
- [ ] Aprobado

### Paso 7b — Marcador de dueño (para colores/packs identicos) ⭐⭐ (NUEVO)
- **Problema**: si P1 y P2 eligen el MISMO color y el MISMO pack, las
  casillas quedan visualmente identicas y no se puede saber de quien es
  cada una. El usuario lo pregunto (2026-11-18).
- **Solucion elegida**: un **marcador de dueño fijo** en cada casilla,
  que NO depende del pack ni del color elegido: un simbolo pequeño en la
  esquina (P1 = punto lleno ●, P2 = aro ○) o un borde sutil, en un color
  fijo contrastante (blanco/negro). Al ser parte de la CANCHA (no del
  pack), siempre distingue a los jugadores aunque compartan look.
- **Archivos**: `CellVisuals.cs` (una capa mas por celda).
- **Prueba**: forzar ambos con naranja + clasico → se siguen
  distinguiendo por el marcador.
- [ ] Pendiente (hacer junto con el paso 7)

### Pasos 8-11 — Objetos y vapor del caldero en el juego ⭐⭐⭐/⭐⭐⭐⭐
- **Que**: portar la receta del menu a world-space, UN objeto por paso,
  calibrando tamaños en UNA CELDA DE PRUEBA de GameScene (no en el menu):
  - **8**: hongos nivel 1. **9**: ojo vivo + hongo grande nivel 2.
  - **10**: hueso + segundo ojo nivel 3. **11**: vapor en lineas.
- **Prueba por paso**: el objeto aparece solo en casillas de P1, respeta
  el espacio del liquido y la franja del nivel; las del bot intactas.
- [ ] 8 aprobado  [ ] 9 aprobado  [ ] 10 aprobado  [ ] 11 aprobado

### Paso 12 — Boom con voladores ⭐⭐⭐⭐⭐
- **Que**: al explotar una casilla de caldero, objetos salen volando en
  diagonales y desaparecen (solo el juego lo muestra: es el premio del
  pack).
- **Prueba**: jugar hasta nivel 4 en una casilla de P1. NO se puede
  probar en celda de prueba — es el ultimo gran paso.
- [ ] Aprobado

### Paso 13 — Sobrescritura por conquista ⭐⭐⭐
- **Que**: cuando el bot conquista una casilla tuya, la casilla se
  reconstruye con el pack del NUEVO dueño (el mismo mecanismo que hoy
  re-tinta por dueño, extendido al pack completo). En las reglas
  actuales la casilla explotada se vacia primero: se ve vacia con el
  pack nuevo y se rellena con las animaciones del nuevo dueño.
- **Prueba**: dejar que el bot te coma casillas → cambian de pack en
  vivo.
- [ ] Aprobado

### Paso 14 — PvP real (futuro, online) ⭐⭐⭐⭐⭐
- Cada cuenta con su pack; al emparejar, cada cliente renderiza las
  casillas con el pack del dueño (los perfiles ya viajan en la nube).
- **Espejo por cliente (analogia Clash Royale del usuario)**: el servidor
  asigna P1/P2, pero CADA pantalla muestra al jugador LOCAL como "yo"
  (abajo) y al rival como "enemigo" (arriba), aunque en el servidor el
  local sea P2. Se implementa con un `viewerId`:
  `SlotLocal(owner) = owner == viewerId ? 1 : 2`, y TODAS las consultas
  de pack/color y la UI usan ese slot mapeado. Asi cada celular ve lo
  mismo ("yo abajo, enemigo arriba") sin cambiar los datos reales.
  El tablero es simetrico: NO hace falta girar las casillas, solo la
  lectura de quien es "local" en la vista.
- Requiere el modo multiplayer (otro proyecto entero). NO empezar hasta
  cerrar 0-13 en el modo vs bot.
- [ ] Pendiente (futuro)

---

## 3. NOTAS DE DISEÑO (resumen de lo aprendido)

- **El pack es global hoy** (`BoardFx.Current`): ese era el bug raiz del
  "todos usaban las burbujas".
- **El pack debe llevar su color** (`liquidColor`): el verde del caldero
  vivia solo en el menu; en el juego el liquido se tintea con el color
  del dueño.
- **Canvas ≠ mundo**: las proporciones del menu no valen en el tablero.
  Calibrar en celda de prueba del juego.
- **La mitad del mecanismo ya existe**: `CellVisual.owner` + `SyncAll`
  re-tintan por dueño hoy; extender eso es "pack por dueño".
- **El perfil ya esta listo**: `EquippedPackId` / `OwnedPacks` viajan en
  Cloud Save por cuenta (con debounce y respaldo local). La parte de
  datos NO se toca de nuevo.
- El diseño v1 del port (CalderoCelda, ordenes de capa, knobs) queda
  guardado en `NOTA_Caldero.md` 8b-bis/8b-ter como referencia, pero el
  plan de aca (por dueño, con celda de prueba) lo REEMPLAZA.

---

## 4. ANATOMIA DEL CASILLERO (la pieza MAS IMPORTANTE del juego)

> Decision del usuario (2026-11-18). El **casillero** es la identidad de
> un jugador: es lo que elige y lo que se guarda. Hoy hay 2 (P1 y P2),
> pero la estructura debe pensarse para **N jugadores** (3vs, 4vs en el
> futuro): NO hardcodear la logica de render a "1 y 2" — los slots son
> indices, y las consultas pasan por `PackFor(slot)` / colores por slot.

### Que contiene un casillero

| # | Contenido | Estado hoy | Futuro |
|---|-----------|-----------|--------|
| 1 | **Nombre/id del pack** (`clasico`, `caldero`, ...) | ✅ `EquippedPackId` / `EquippedPackId2` | Catalogo crece con `RegisterPack` |
| 2 | **Color** del jugador (propio o automatico) | ✅ `PlayerColorMode`/`CustomColor` por slot + marca `Color2Elegido` | Colores nuevos, rueda por slot |
| 3 | **Efectos**: burbujas, vapor, llamas, etc. | ✅ `FxPack.burbujasOn` por pack: clasico NO, especiales SI (paso 3b) | Caldero trae sus burbujas/vapor propios por codigo (paso 7+) |
| 4 | **Objetos**: hongos, ojos, hueso, etc. | 🟡 en el menu; el juego los portara por pasos (8-10) | Objetos propios por pack |
| 5 | **Marco** personalizado por pack | ❌ hoy solo la **tapa** (0tapa) | **Anotado**: el usuario implementara un marco distincto por pack (paso 15) |
| 6 | **Sonido de explosion propio** | ✅ `FxPack` lee A/B + modo + volumen (paso 3b) | Clips propios por pack |
| 7 | **Animaciones** (nivel 1-3 + boom) | 🟡 4 clips **compartidos** por todos los packs hoy | Cada pack podra traer las suyas: el diseno NO asume que siempre son las mismas (pasos 6-12) |
| 8 | **Musica / OST del pack** | ❌ no existe | Tema propio por pack: caldero = metal de Halloween, acuario = jazz calma, etc. Suena **durante la partida cuando vas ganando** y **al terminar**, y **cambia segun quien toma el control** (paso 16) |

### Como se conecta con el tablero

- El tablero es **neutral** (la cancha): no guarda packs, guarda solo
  `owner` por casilla.
- Cada casilla resuelve en vivo: `PackFor(owner)` → Pack →
  color / efectos / objetos / marco / sonido / animaciones / OST.
- Al conquer una casilla cambia el `owner` → la casilla se redibuja con
  el casillero del nuevo dueño (paso 13).
- En PvP (paso 14) se agrega `viewerId`: cada pantalla mapea
  `owner == viewerId ? 1 : 2` para ver siempre al local como "yo"
  (espejo tipo Clash Royale), sin tocar los datos reales.

### Filosofia CLASICOS vs PACKS PREMIUM (decision del usuario)
- Los **clasicos** son la LINEA BASE y la comparten TODOS:
  nombre/classic, **color plano** (rosa/naranja/rueda), **sin burbujas**,
  **mismo sonido de explosion** y (en el futuro) **misma musica**.
  No es un castigo: es la identidad "clasica". El usuario eligio el
  sonido de la **ranura B** (`boomSound: 1` en GameScene) como el de los
  clasicos; hay dos ranuras A/B y un modo (0=solo A, 1=solo B,
  2=alternar, que era solo de prueba). El pack clasico hereda el modo 1.
- Los packs **especiales** (caldero de bruja, acuario, futuros) se
  **compran aparte** y son PREMIUM: traen burbujas/vapor propios,
  objetos (hongos/ojos/hueso), sonido de explosion propio, animaciones
  propias y OST propia. Se pueden **presumir** frente a los demas.
- Proposito: dar **valor a las monedas y diamantes** y a la **tienda**.
  El pack especial debe sentirse importante e incentivar a ganar para
  comprarlo y ponerlo. El clasico siempre queda disponible y gratis.

### Regla de oro para el diseno del casillero

> Un casillero **nunca** debe depender de que haya exactamente 2
> jugadores. Los campos se guardan por slot (hoy 1 y 2), y el
> render/audio consultan por slot. Asi agregar un 3er o 4to jugador
> es agregar datos, no reescribir el juego.
