# NOTA — PLAN: PACKS INDIVIDUALISTAS POR JUGADOR

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
| 3 | Decidir el pack del BOT (constante "clasico") | ⭐ facil | No |
| 4 | UI de seleccion de packs en el menu (equipar/quitar, por cuenta) | ⭐⭐ medio | No (solo menu) |
| 5 | Compra de packs con monedas/gemas | ⭐⭐ medio | No (solo menu) |
| 6 | Registrar el pack "caldero" en el catalogo (datos, sin render) | ⭐⭐ medio | Poco |
| 7 | Render por casilla segun dueño (cristal + liquido + burbujas) | ⭐⭐⭐ dificil | SI |
| 8 | Objetos nivel 1 (hongos) en world-space, calibrados en celda de prueba | ⭐⭐⭐ dificil | SI |
| 9 | Objetos nivel 2 (ojo vivo + hongo grande) | ⭐⭐⭐⭐ dificil | SI |
| 10 | Objetos nivel 3 (hueso + segundo ojo) | ⭐⭐⭐⭐ dificil | SI |
| 11 | Vapor en world-space | ⭐⭐⭐⭐ dificil | SI |
| 12 | Boom con voladores (nivel 4) | ⭐⭐⭐⭐⭐ muy dificil | SI |
| 13 | Sobrescritura del pack al conquistar | ⭐⭐⭐ dificil | SI |
| 14 | PvP real: cada cuenta con su pack (futuro, online) | ⭐⭐⭐⭐⭐ muy dificil | SI |

Regla de la casa: los pasos 1-6 son "seguros" (el juego queda jugable
aunque se corte ahi). Desde el 7 en adelante se entra en render real —
a partir de ahi, si algo se rompe, se revierte al paso anterior sin
pena y se reintenta con celdas de prueba.

---

## 2. DETALLE DE CADA PASO

### Paso 0 — Checkpoint (el usuario)
- [ ] Commit git del proyecto entero con el juego funcionando como hoy
      (BASE SEGURA post-emergencia).
- [ ] Verificado: Play en GameScene = casillas de cristal con liquido
      animado, sin errores en consola.

### Paso 1 — Color de liquido propio del pack ⭐
- **Que**: `FxPack` gana `liquidColor` (y `liquidColorP2` para el rival,
  o la regla de opuestos que ya usa BoardFx). `CellVisuals` tintea el
  liquido con el color del PACK en vez del color global por dueño.
- **Archivos**: `FxPacks.cs`, `CellVisuals.cs`, `BoardFx.cs` (leve).
- **Prueba en Play**: con "clasico" se ve IDENTICO a hoy (rosa/naranja).
- [ ] Aprobado

### Paso 2 — Pack por dueño (ruteo) ⭐
- **Que**: `BoardFx.Current` (global) se reemplaza por
  `BoardFx.PackFor(int owner)`. P1 = pack del perfil; P2 = pack del bot.
  `CellVisuals` pregunta el pack POR CASILLA (ya conoce `owner`).
- **Archivos**: `BoardFx.cs`, `CellVisuals.cs`, `GameManager.cs` (los
  hooks `LevelFx/ExplosionFx` pasan el owner o el pack).
- **Prueba en Play**: ambos jugadores = "clasico" → IDENTICO a hoy.
- [ ] Aprobado

### Paso 3 — Pack del BOT ⭐
- **Que**: decidir como se elige el pack del bot. Opcion simple: una
  constante `BotPackId = "clasico"` (luego puede venir de Remote Config
  o de la dificultad). Queda anotado, no bloquea nada.
- [ ] Decidido y anotado

### Paso 4 — Seleccion de packs en el MENU ⭐⭐
- **Que**: panel de packs con los botones (el caldero del menu ya existe
  como vista). Toggle: clic equipa / clic quita; los botones basicos
  quitan el especial. Todo via `UGSProfileManager` (Unlock/Equip — ya
  existen y ya guardan en nube + local con debounce).
- **Archivos**: SOLO scripts de `Assets/scrips/packs/` y UI del menu.
  El juego NO se toca aca.
- **Prueba**: consola muestra equipar/quitar; perfil persiste al
  reabrir. El juego sigue mostrando clasico (aun no consume el pack).
- [ ] Aprobado

### Paso 5 — Compra de packs ⭐⭐
- **Que**: comprar con `SpendCurrencyAsync` (existe) antes de
  desbloquear. Precios por pack (tabla en esta nota o Remote Config).
- **Prueba**: comprar, quedar `OwnedPacks` actualizado, monedas bajan,
  persiste en nube.
- [ ] Aprobado

### Paso 6 — Registrar "caldero" (solo datos) ⭐⭐
- **Que**: `FxPackCatalog.RegisterPack(new FxPack("caldero", ...))` con
  su `liquidColor` VERDE y las burbujas (sprites). **Sin render nuevo**:
  equipar caldero todavia se ve clasico (el render por casilla viene
  en el paso 7).
- **Archivos**: un loader chico en `Assets/scrips/packs/`.
- **Prueba**: equipar caldero NO rompe nada; se ve clasico; consola
  lista el pack registrado.
- [ ] Aprobado

### Paso 7 — Render por casilla ⭐⭐⭐
- **Que**: `CellVisuals` arma cada casilla con el pack de SU dueño:
  cristal, clips y color del pack. Casilla de P1 con caldero = liquido
  verde; casilla del bot = clasico normal.
- **Archivos**: `CellVisuals.cs` (el gordo), `BoardFx.cs`.
- **Prueba**: P1 = caldero, bot = clasico → tus casillas verdes, las
  del bot rosa/naranja como siempre. Con ambos clasico = IDENTICO.
- [ ] Aprobado

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
