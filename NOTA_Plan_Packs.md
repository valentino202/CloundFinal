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

### Paso 5 — Compra de packs ⭐⭐ (IMPLEMENTADO)
- `TiendaPacks.cs`: vende el caldero (200 monedas, 0 gemas por defecto),
  descuenta con `SpendCurrencyAsync`, desbloquea con `UnlockPackAsync`,
  guarda en nube, actualiza textos y estado. Reembolsa si falla el
  desbloqueo. Balances: textos propios o `UIManager.RefreshCurrencyTexts`.
- `SwatchCaldero` ya NO desbloquea automaticamente: si no compraste el
  pack, el boton se ve atenuado (`CanvasGroup.alpha=0.45`,
  `interactable=false`) y no se puede equipar. Al comprarlo se habilita.
- **Setup en el Inspector**: poner `TiendaPacks` en el GameObject
  `Tienda_Caldero` y arrastrar: `botonComprar` (Boton_comprar),
  `textoBoton` (el de "COMPRAR"), `textoEstado` (el de "comprado"),
  `textoMonedas`/`textoGemas` (CoinsText_cald / GemasText_cald = SALDO),
  `textoPrecioMonedas`/`textoPrecioGemas` (oro_cald / diamante_cald =
  PRECIO FIJO 1000/500, se escriben solos). NO hay texto de precio suelto.
  Estado: "Monedas/gemas insuficientes" si no alcanza; "COMPRADO ✅" de
  forma persistente una vez comprado.
- ** economia NO se toca** (usuario): la economia general del juego
  (`UGSEconomy.Coins/Gems`, monedas/gemas ganadas al jugar) y sus textos
  generales (`UIManager.coinsTexts/gemsTexts`) quedan EXACTAMENTE como
  estaban. La compra del caldero **descuenta de ese saldo global** via
  `SpendCurrencyAsync` y guarda en la nube. El script de la tienda SOLO
  muestra los valores del caldero (precio 1000/500 en `oro_cald`/
  `diamante_cald` y saldo en `CoinsText_cald`/`GemasText_cald`, lectura del
  perfil). No hay saldos duplicados ni sincronizaciones nuevas.
- El boton Tienda del menu llama a `UIManager.ShowShop`.
- **Vista previa animada** (usuario: "no imagen, el personalizado con sus
  animaciones"): `TiendaPacks` tiene `selectorCaldero`, `slotEnTienda` y
  `slotEnPersonalizacion`. Al abrir la tienda, **mueve el MISMO GameObject**
  `Selector_Caldero` al slot de la tienda (conserva llenado, burbujas,
  vapor, hongos, ojo, hueso). Al abrir Personalizacion (`UIManager.
  ShowCustomization`) lo devuelve a su slot. No es una copia ni una imagen.
- **Setup**: arrastrar `Selector_Caldero`, crear un slot (RectTransform)
  dentro del ShopPanel y el slot original dentro del CustomizationPanel.
- **SELECCION SOLO EN PERSONALIZACION** (usuario): `SwatchCaldero` tiene
  `slotEnTienda` y la propiedad `EnTienda`. `OnPointerUp` y `SeleccionarPack`
  hacen `return` si esta en la tienda → ahi el caldero SOLO se ve (con sus
  animaciones); no equipa, no guarda, no selecciona. El unico lugar que
  puede equiparse para las partidas es Personalizacion.
- [ ] Aprobado por el usuario (PENDIENTE de armar la UI y probar)
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

### Paso 8 — Objetos nivel 1: los DOS HONGOS ⭐⭐⭐ (IMPLEMENTADO)
- **Fuente de los valores**: la jerarquia del menu (usuario: "los datos
  de menu en la jerarquia son los correctas"). Boton `Selector_Caldero`
  = 100x100 px → sizes: Hongo1 14px (0.14 celda), Hongo2 7px (0.07),
  hongo grande 17px, ojo 23px, hueso 65px. Datos de movimiento leidos:
  Hongo1 franja 0.239, paseo 10px/s (0.1 celda/s), giro 22, respiro
  tamano 0.467; Hongo2 franja 0.35, giro -22, respiro 0.161.
- **Que hecho**: `FxPack` gana los sprites (cargados por nombre desde
  `Assets/imagenes`) y la receta de hongos. Nuevo `CalderoCelda.cs`
  (en `Assets/scrips/packs/`): por celda del pack crea los dos hongos,
  paseo libre dentro de la franja, giro, respiro y tinte sumergido leido
  del liquido en vivo. `CellVisuals` lo crea cuando la celda pasa a ser
  de un pack con objetos (tablero nace neutral) y le pasa el nivel.
- **CORRECCION tras la prueba**: los hongos aparecian DETRAS del liquido
  (mismo `sortingOrder`, mismo Z). Camara 2D en `z = -10` mirando a +Z,
  asi que "mas cerca" es z MENOR: ahora `localPosition.z = -0.01` con la
  misma capa del liquido → se ven DENTRO del brebaje; el vidrio
  (`glassOrder = 1`) sigue encima. Tambien se agrego el RESPIRO del
  tinte sumergido del menu (fuerza 75%..100% de 0.25, vel 0.8).
  Luego el usuario pidio pasar TODO lo del boton: se agrego ARCOIRIS
  (`hongosArcoiris = true`, vel 0.5, HSV sat 0.8 / val 0.9±0.1),
  fuerza sumergida 1.0 y opacidad 0.82 — valores leidos del menu.
- **CORRECCION final tras la captura del usuario** (hongos diminutos y
  abajo): el PNG no mide 1 unidad → ahora se NORMALIZA por
  `sprite.bounds` para que el tamano sea fraccion REAL de la celda, y
  se agranda un poco (0.2 / 0.12 de celda). El rango de paseo usa las
  SUPERFICIES REALES del liquido (`surface1/2/3` del Inspector, con
  margen de 0.03) en vez de la franja vieja, y en `SetNivel` el objeto
  arranca YA dentro del liquido (no en el centro).
- **Ajuste final del usuario**: en nivel 1 los hongos se ven grandes y
  salen de abajo, y no suben. Ahora el tamano es POR NIVEL: factor 0.65
  en nivel 1 (≈0.13/0.08 de celda, como el menu) y 1.0 en niveles 2-3
  (0.2/0.12). El rango de paseo usa el fondo real de la celda y el techo
  con margen minimo (0.01) para que recorran mas hacia arriba.
- **CORRECCION de la captura**: el hongo asomaba por DEBAJO del verde
  (el borde del cristal y el `liquidLift` comen abajo). El fondo del
  paseo ahora es `-celda * 0.42` (fondo real del verde), no el borde de
  la celda `-0.5`.
- **CORRECCION del usuario** (noto que todos los respiros se reiniciaban
  al crear una celda o subir otra a nivel 2): `SyncAll` llama
  `ApplyState` en TODAS las celdas por cada jugada. Ahora `CellVisuals`
  solo llama `SetNivel` si `changed` (owner/nivel) o si cambio el pack
  de objetos. `CalderoCelda.SetNivel` ya NO teletransporta a los hongos
  vivos: conservan destino, fase y ritmo; solo ajusta su tamano al nivel
  y los recorta contra los nuevos limites. Los objetos NUEVOS del nivel
  2 arrancan con su propia fase, sin sincronizarse con los viejos.
- **Prueba**: P1=caldero → al poner un punto (nivel 1) aparecen los dos
  hongos DENTRO del liquido en sus casillas; P2 no tiene nada.
- [ ] Aprobado por el usuario (PENDIENTE de probar)

### Paso 9 — Nivel 2: ojo vivo + hongo grande ⭐⭐⭐ (IMPLEMENTADO)
- **Valores del menu**: Ojo 23px (0.33 celda), `bobVertical 14.8`,
  giro 22, tinte sumergido 1, `tenirHijos=0` (la iris queda viva),
  arcoiris 0. Hongo grande 17px (0.24 celda), giro -30, respiro
  tamano 0.482. Iris: rojo `(1, 0.13, 0.13)`, desplazamiento 0.22,
  extraLateral 1.4, suavidad 0.15, cambios 0.7-2.2s, 5 miradas
  (central + laterales/verticales espejadas con el mismo sprite).
- **Que hecho**: `FxPack` gana los sprites (cargados por nombre) y todos
  esos knobs. `CalderoCelda` crea el ojo con su **iris hija** y las 5
  miradas (cambio aleatorio, espejo por scale negativo, barrido lateral
  mas largo, Lerp suave). Objetos del nivel 2 arrancan inactivos y se
  activan al subir, **sin reiniciar** los hongos del nivel 1.
- **CORRECCION de la captura**: la iris salia diminuta (~27%) porque es
  hija del ojo y su `localScale` se multiplica por la escala del padre.
  Ahora se divide por `o.escala` para que su tamano MUNDIAL sea el 68%
  real de la esclerotica, como en el menu. El usuario la prefirio un
  La iris vuelve a 0.68 (proporcion original, la correcta). El ajuste
  era solo del OJO COMPLETO: `ojoTamano = 0.28` de celda (era 0.33).
  Ambos valores son ajustables en `FxPack`.
- **Prueba**: nivel 2 → aparecen el ojo con la iris bien proporcionada
  mirando a todas partes y el hongo grande; nivel 1 sigue igual.
- [ ] Aprobado por el usuario (PENDIENTE de probar)

### Paso 10 — Nivel 3: hueso + segundo ojo azul ⭐⭐⭐ (IMPLEMENTADO)
- **Valores del menu**: hueso 65px (finalmente `huesoTamano = 0.45` de
  celda — el usuario lo vio grande; ajustable en el Inspector), antes 0.6
  desborde), paseo 14px/s por los BORDES, NO gira (se meca 44.7°, 0.366
  Hz), `sumergido=0` (parte de arriba normal) + copia `Hueso_Agua` con
  fuerza 1 y opacidad 1 teñida del liquido con respiro. Segundo ojo 11px
  (0.14 de celda) con iris AZUL `(0, 0.192, 1)`.
- **Que hecho**: `FxPack` gana los sprites (Huesito cargado por nombre),
  tamaños y knobs. `CalderoCelda` crea ambos objetos con `nivelMinimo=3`,
  el hueso con paseo por bordes/vaiven y su copia sumergida como hija que
  sigue su transform y respira el verde. El ojo 2 reutiliza la logica de
  iris del ojo 1 con color azul.
- **CORRECCION tras la prueba del usuario** (hueso todo verde y capas
  desordenadas):
  1. La copia sumergida usa un **`SpriteMask` con las medidas reales de
     `ZonaAgua` del menu** (100 × 78.77 px de boton 100, centrada en
     y=-11.3 → en juego: `celda × 1 × 0.7877`, y `-celda × 0.113`), así
     solo se ve **bajo el agua** como el `RectMask2D` — el hueso de
     arriba queda con su color normal. La iris azul sigue siendo HIJA de
     base_ojo2, con Z local `-0.0005` para no adelantarse al hueso
     (antes parecia flotar separada). Correccion de Z: la copia va
     local `-0.002` (delante del hueso `-0.014`, detras del ojo `-0.017`).
  2. Se respeta el **orden de hermanos de la jerarquia** del menu
     (`Liquid → Hongo3 → Hongo2 → ojo2 → hueso → Hongo1 → copia → ojo →
     Crystal`) mediante profundidades Z distintas (misma capa de orden
     que el liquido; el vidrio sigue en `glassOrder` encima de todo).
- [ ] Aprobado por el usuario (PENDIENTE de probar)

### Paso 11v2 — Efectos world-space con bounds reales ✅ (REINTENTO)
> Tras el revert del primer intento, se rehizo con `BurbujasVaporCaldero.cs`:
> - **Causa raiz de los bugs anteriores**: en el menu todo son PIXELES del
>   RectTransform del Liquido (100px); en el juego no hay RectTransform,
>   asi que copiar numeros hacia que las burbujas salieran del cristal.
>   Ahora el "rect" del liquido se mide con `sprite.bounds * localScale`
>   del SpriteRenderer real (ancho/alto en unidades de mundo), y el fondo
>   es `-alto*0.5` (pivote centrado, igual que el menu).
> - Logica COPIADA del menu: tasa fija `* densidadEfectos`, fade in 0.12s,
>   crecimiento 0.85→1.15, pop al llegar a superficie, dos capas con
>   `porcentajeDelante=0.875` y `sesgoProfundidad=0.206`, variacion por
>   burbuja, respiracion del color real. Vapor: lineas de 6 puffs nacen
>   juntas dentro del verde (`nacerAbajo=0.5`), suben, crecen, se congelan
>   al tope y mueren. Sprites generados por codigo (4 anillos, 3 puffs).
> - **Limpieza**: `SetNivel(0)` o cambio de dueno libera TODO al pool →
>   nunca queda humo/burbujas congeladas. El emisor simple (`foam`) del
>   clasico se detiene con StopEmittingAndClear cuando el pack trae efectos.
> - Requiere `TieneObjetos` (el pack caldero tiene sprites) para crear el
>   componente.

### Paso 11 — ⛔ REVERTIDO (primer intento, historical)
> El usuario pidio volver atras: el port de `BurbujasCalderoUI` +
> `VaporCalderoUI` (`EfectosCalderoCelda.cs`) salia mal (burbujas fuera
> del cristal, vapor congelado, burbujas mal en todos los niveles).
> Se BORRO `EfectosCalderoCelda.cs` y `CellVisuals` volvio al emisor
> simple de burbujas (`SetupBubbles` / `SetBubbleRate`), sin vapor.
> El juego queda como antes de este paso. NO reintroducir de golpe.

### Paso 11 (historial) — Burbujas del menu + vapor en world-space ⭐⭐⭐⭐
- **Que hecho**: nuevo `EfectosCalderoCelda.cs` que porta
  `BurbujasCalderoUI` y `VaporCalderoUI` completos:
  - **Burbujas 2 planos**: capa atras (`z=-0.005`, detras de los objetos)
    y capa delante (`z=-0.018`, por encima del ojo pero detras del vidrio),
    con `porcentajeDelante=0.3` y `sesgoProfundidad=0.7` → las de delante
    marcadas, las de atras tenues. Variacion de tamano/alpha/mezcla/ritmo
    por burbuja, fade in, crecen al subir y **pop** al llegar a la superficie.
  - **Vapor en LINEAS**: bloques de 4 puffs nacen juntos dentro del verde
    (`nacerAbajo=0.5`), suben como columna, crecen, respiran el color real,
    se congelan al tocar el techo (`topeVapor=0.25`) y se desvanecen.
  - Sprites de anillos/puffs generados por codigo (4 y 3 variantes) con
    pool de objetos — igual que el menu, sin assets nuevos.
- `FxPack` gana todos los knobs de variacion/planos/vapor del menu.
  `CellVisuals` crea el componente cuando el pack trae efectos y apaga
  el emisor simple (`SetBubbleRate` early-return).
- **CORRECCION de velocidad/intensidad**: el usuario comparo con el menu
  y los defaults estaban lejos. Se copiaron los valores REALES del
  `SwatchCaldero` de SampleScene (lineas 1793-1816) al pack:
  burbujas 15/s, tamaño 0.0538, subida 0.276, deriva 0.005, alpha 0.231,
  mezcla 1, respiro 0.61, variacion alpha 2 / mezcla 0.299 / respiro 0.72,
  87.5% delante, sesgo 0.206; vapor 1.94 lineas/s, 6 puffs, ancho 0.606,
  tamaño 0.145, crecimiento 1.022, alpha 0.213, mezcla 0.729, techo 0.08.
  Ademas las franjas por nivel del menu (0.25/0.5/0.85) se guardan en el
  pack y se usan para efectos (antes se usaban los `surface` del Inspector
  del juego, que son distintos: 0.32/0.59/0.89).
- **CORRECCION "tortuga"**: no era FPS (no hay `timeScale` ni
  `targetFrameRate` en el codigo, y todo usa `unscaledDeltaTime`). Era la
  TASA: el menu siempre esta lleno (nivel 3 = 15/s) pero las casillas del
  juego viven en nivel 1, y `nivel/3` las dejaba en 5/s. Ahora
  `efectosTasaNivel1 = 5` compensa la diferencia de recorrido: en el menu
  (nivel 3) una burbuja vive ~3s → ~46 vivas; en el juego nivel 1 solo
  recorre 0.2u (~0.6s), necesita ~3x de tasa para igualar la DENSIDAD
  visual. Interpolamos hasta 1x en nivel 3. Perilla `velocidadEfectos`
  (default 1) multiplica la subida si aun se ven lentos.
- **CORRECCION de burbujas en TODOS los niveles** (usuario: "estan mal
  en todos"): comparando `Nacer()` del menu contra mi codigo:
  1. **Fondo**: el menu nace en `-h*0.5` (fondo real del liquido) y usa
     `fondo = -h*0.5` para el crecimiento; yo usaba `-celda*0.42` fijo →
     recorrido y `prog` malos en todos los niveles. Ahora `FondoLiquido()`
     = `-celda*0.5`, identico.
  2. **Escala del sprite**: el menu pone `sizeDelta = sx` (pixeles del
     boton de 100); yo ponia `localScale = sx` sobre un sprite de 64px
     (0.64u) → burbujas/puffs 64% mas grandes. Agregue `EscalaSprite()`
     para normalizar por `sprite.bounds` (1/nativo), igual que los objetos.
- **CORRECCION del humo congelado** (captura de celdas nivel 1/2):
  `EfectosCalderoCelda.Update` hacia `return` cuando `nivel <= 0`, dejando
  los puffs/burbujas VISIBLES en su ultimo estado (congelados). El menu no
  tenia este problema porque siempre esta lleno. Ahora, cuando no hay
  nivel, se LIBERAN todos los puffs/burbujas al pool antes de volver.
- **CICLO RAFAGA/DESCANSO** (usuario: "saturo la vista, que descansen 2s"):
  `FxPack.efectosRafaga=2` y `efectosDescanso=2`. En la rafaga nacen
  burbujas/vapor; en el descanso no nacen nuevos y los vivos terminan solos
  (transicion suave). Al limpiar la celda el ciclo vuelve en rafaga. Baja la
  fatiga visual (numero+cristal+objetos+burbujas+vapor a la vez).
- **CORRECCION DEFINITIVA del usuario** (mostro el Inspector de
  "OPCIONES DE EFECTOS": valores identicos en los 3 niveles, solo cambia
  la franja del liquido; multiplicadores generales en 1):
  - TASA CONSTANTE: `bubbleRate * densidadEfectos` (15/s) y
    `vaporLineas * densidadEfectos` (1.94/s) FIJOS en cualquier nivel.
    Se elimino el escalado `nivel/3` (estaba mal: el menu no baja la tasa
    en nivel 1). Lo que cambia es SOLO la superficie (0.25/0.5/0.85),
    por eso en nivel 1 se ven menos burbujas: recorren menos distancia,
    no porque nazcan mas lento.
  - Los 4 multiplicadores del Inspector (`densidadEfectos`,
    `tamanoEfectos`, `velocidadEfectos`, `opacidadEfectos`, todos en 1)
    ahora estan en `FxPack` y se aplican en nacimiento/tamano/subida/alpha.
  - Vapor CONSTANTE con sitio LIMITADO junto a la superficie:
    `tope = Min(superficie + celda * vaporTecho, celda * 0.5)` =
    VaporCalderoUI.Update linea 211.
- **AUDITORIA final (todos los valores coinciden con el menu)**: 15
  burbujas/s, tamaño 0.0538, subida 0.276, deriva 0.005, alpha 0.231,
  mezcla 1, respiro 0.61, variaciones 2/0.299/0.72, 87.5% delante,
  sesgo 0.206; vapor 1.94 lineas/s, 6 puffs, ancho 0.606, tamaño 0.145,
  crecimiento 1.022, alpha 0.213, mezcla 0.729, techo 0.08, franjas
  0.25/0.5/0.85. Lo que faltaba no eran numeros sino GEOMETRIA: el menu
  siempre esta en nivel 3 (superficie al 85%, mucho aire arriba); en el
  juego las casillas son nivel 1 (superficie al 25%), y con `techo=0.08`
  el vapor quedaba en una rendija de 0.1u. El techo ahora usa el aire
  real de la celda (`Max(superficie + techo*celda, 0.42*celda)`), que en
  nivel 3 coincide exactamente con el menu.
- **CORRECCION del vapor "verguenza"**: mis puffs usaban un relleno tenue
  con ruido; el menu (`VaporCalderoUI.Puffs`, linea 452) usa una
  **gaussiana `exp(-d·k)`** con durezas 2.6/4.2/6.5 y textura 96px, sin
  ruido → nube densa al centro. Se copio EXACTO, mas `factorT 0.7-1.4` y
  giro ±12 del menu. Burbujas: tasa de nivel 1 subida a 5x para llenar
  la franja corta.
- **CORRECCION de la captura**: el vapor aparecia en TODAS las casillas
  (incluso clasicas) porque `vaporLineas = 1.94` es default de `FxPack`,
  asi que el clasico tambien cumplia `vaporLineas > 0`. Se agrego el flag
  explicito **`vaporOn`** (false en clasico, true en caldero) y la
  condicion ahora es `burbujasOn || vaporOn`. Ademas, si una casilla
  caldero es conquistada por el bot, `SetNivel(0)` apaga los efectos
  colgados.
- **Prueba**: casilla de caldero → burbujas delante y detras de los objetos
  + columnas de vapor subiendo, MISMA velocidad/intensidad que el menu.
- [ ] Aprobado por el usuario (PENDIENTE de probar)
- **Que**: portar la receta del menu a world-space, UN objeto por paso,
  calibrando tamaños en UNA CELDA DE PRUEBA de GameScene (no en el menu):
  - **8**: hongos nivel 1. **9**: ojo vivo + hongo grande nivel 2.
  - **10**: hueso + segundo ojo nivel 3. **11**: vapor en lineas.
- **Prueba por paso**: el objeto aparece solo en casillas de P1, respeta
  el espacio del liquido y la franja del nivel; las del bot intactas.
- [ ] 8 aprobado  [ ] 9 aprobado  [ ] 10 aprobado  [ ] 11 aprobado

### Paso 12 — ⛔ DESACTIVADO (se retomara mejor en el futuro)
> El usuario pidio sacar el boom con voladores: no termino de convencer.
> `BoomVoladores.cs` queda guardado, pero `CellVisuals.PlayBoom` Ya NO lo
> dispara (codigo comentado). El boom sigue siendo clip + sonido + destello
> de siempre.

### Paso 12 (historial) — Boom con voladores ⭐⭐⭐⭐⭐
- Nuevo `BoomVoladores.cs`: al explotar una casilla del caldero salen
  `boomObjetos` (6) sprites de objetos (hongos/ojo/hueso) en diagonales
  laterales al azar, con giro, gravedad leve y desvanecimiento. Se
  auto-destruye al terminar. Solo si `pack.TieneObjetos`.
- `FxPack` gana `boomObjetos`, `boomVelocidad`, `boomVida`. Clip/sonido/
  destello los maneja `CellVisuals.PlayBoom` como siempre.
- **Prueba**: jugar hasta nivel 4 en una casilla de P1 → objetos volando
  desde la explosion. No se puede probar en celda de prueba.
- [ ] Aprobado por el usuario (PENDIENTE de probar)

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

## 3b. CONVERSION DE MEDIDAS MENU → JUEGO (lineal)

> Pedido del usuario: medir los objetos en el menu y hacer la operacion
> de dimensiones lineales, en vez de adivinar tamaños.
>
> Boton `Selector_Caldero` = **100 px**; celda del juego = **1.2 unidades**.
> `1 px = 0.012 unidades`; `fraccion de celda = px / 100`.
> Helper: `FxPackCatalog.FraccionDeCeldaDesdePx(px)`.

| Objeto | Rect en menu | Fraccion de celda (px/100) |
|---|---|---|
| Hongo 1 | 14×14 px | **0.14** |
| Hongo 2 | 7×7 px | **0.07** |
| Hongo grande | 17×17 px | **0.17** |
| base_ojo (ojo 1) | 23×23 px | **0.23** |
| base_ojo2 (ojo 2) | 11×11 px | **0.11** |
| Hueso | 65×65 px | **0.65** |

El usuario-confirmo estos valores y pidio usarlos EXACTOS (antes habia
ajustes a ojo que dejaban el hueso demasiado chico). Se asignan en el
constructor del catalogo con `FraccionDeCeldaDesdePx(px)` y se normaliza
por `sprite.bounds` para que el arte dibujado ocupe esa fraccion real.

**HOMOTECIA (escalado uniforme, pedido del usuario)**: el factor
`k = objetosEscalaGlobal` se aplica a TAMAÑOS **y** POSICIONES relativas
desde el origen (centro de la celda): `x_nuevo = 0 + k·(x_original - 0)`.
En `CalderoCelda` eso significa que `K` escala: diámetro, márgenes de
paseo (`±0.45·celda·K`), fondo del verde (`-0.42·celda·K`), techo
(`topeY·K`), bob y velocidad. El área cambia como `k²` y el volumen como
`k³` (nota del usuario). La mascara `ZonaAgua` NO se escala: es el
"recipiente" (la cancha), no parte del conjunto de objetos.

**Perillas** (para ajustar sin romper las proporciones):
- `objetosEscalaGlobal` = 1 → escala TODOS los objetos juntos (k).
- `factorTamanoNivel1` = 1 → factor extra solo para la franja fina del
  nivel 1 (el usuario lo habia pedido mas chico antes; ahora queda 1 =
  identico al menu y se puede bajar si se ve cargado).

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
