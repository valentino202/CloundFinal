# MANUAL: CÓMO PASAR UN PACK DEL MENÚ AL JUEGO

> Documento vivo. Recopila TODO lo que aprendimos portando el **caldero
> de bruja** (nov-2026) para no repetir errores con packs futuros
> (acuario, metal, etc.). Complemento de `NOTA_Plan_Packs.md` (plan de
> pasos) y `NOTA_Caldero.md` (historia del pack).

---

## 0. PRINCIPIO RECTOR

```
MENÚ  = herramienta de AUTORÍA (canvas, UI, fracciones del botón 100×100)
JUEGO = world-space (celdas de 1.2 unidades, SpriteRenderer, cámara en z=-10)
NUBE  = solo la ELECCIÓN (EquippedPackId), NUNCA la receta visual
```

El botón del menú es donde se **calibra a ojo**. Esos valores se
**miden y convierten** al juego; la nube guarda solo *qué pack eligió
cada jugador*. Nunca subir la receta a la nube (se desactualizaría).

---

## 1. CHECKLIST DEL PORT (en este orden)

### Paso A — Datos del pack
- [ ] Registrar el pack en `FxPackCatalog` con `id`, nombre y 4 llaves.
- [ ] Si no tiene arte aún, el juego **no se rompe**: `CellVisuals.Update`
      ignora packs con `HasArt = false` (salvaguarda contra el fallback
      de cuadrados planos).
- [ ] `usaColorPropio` + `colorLiquidoPropio` si el pack tiene color propio
      (el verde del caldero). El pack SIEMPRE lleva su color: en el juego
      el líquido se tintea con el **pack del dueño**, no con un global.

### Paso B — Efectos (burbujas / sonido)
- [ ] Los efectos viven en `FxPack`: `burbujasOn`, `bubbleRate`, `bubbleSize`,
      `bubbleRise`, `bubbleDrift`, `bubbleAlpha`, `sonidoBoom`, `boomVolume`.
- [ ] NO hay switch global en el tablero. El pack decide.
- [ ] `PackEfectos(owner)` (efectos/color, sin exigir arte) ≠
      `PackDeCelda(owner)` (clips, exige `HasArt`). Mezclarlos rompe algo.
- [ ] El tablero nace **neutral** (`owner = 0`): crear el emisor SIEMPRE
      (callado si el pack no trae burbujas) y reconfigurar en `ApplyState`
      cuando cambia el pack del dueño (`bubblePackId`).
- [ ] `SyncAll` llama `ApplyState` en TODAS las celdas por cada jugada:
      **frenar** las operaciones con `changed` / `idPack != idGuardado`,
      o todos los objetos se teletransportan al unísono.
- [ ] `SetupBubbles` debe `ps.Stop(true, StopEmittingAndClear)` ANTES de
      tocar `main.duration` (Unity lo prohíbe con partículas vivas).
- [ ] Sonido: dos ranuras A/B + modo 0/1/2; normalizar volumen por pack
      (`boomVolume` propio) y campos nuevos del Inspector = defaults del
      código aunque la escena tenga valores viejos.

### Paso C — Objetos (medir, no adivinar)
1. **Medir** los `RectTransform` de los hijos del botón en la escena del
   menú (ej: hongo1 14×14 px, hueso 65×65 px sobre un botón de 100×100).
2. **Convertir por dimensión lineal**:
   `fracción de celda = px / 100`  → helper `FraccionDeCeldaDesdePx(px)`.
3. **Normalizar por sprite**: el PNG no mide 1 unidad
   (`diámetro = celda · fracción`, `escala = diámetro / sprite.bounds`).
4. **Homotecia** con `k = objetosEscalaGlobal`: escalar tamaños **y**
   posiciones relativas desde el centro (`x·k`, `y·k`), además de bob y
   velocidad. El área va como `k²`, el volumen como `k³`.
5. **Capas**: leer el orden de `m_Children` en el YAML del menú
   (de atrás hacia adelante) y replicarlo con **Z** (cámara en -10 →
   **Z más negativo = más cerca**). El vidrio va en `glassOrder` encima.
6. **Copia sumergida**: el `RectMask2D` del menú (`ZonaAgua`, medidas reales)
   se reemplaza por un `SpriteMask` con esas mismas medidas.
7. **Iris**: es **hija del ojo**; su `localScale` se divide por la escala
   del padre (si no, doble escalado → puntito rojo). Su Z local es
   apenas `-0.0005` para no adelantarse al hueso.
8. Objetos por nivel con `nivelMinimo`; al subir de nivel los viejos
   **conservan** fase/destino, solo se activan los nuevos.

### Paso D — Selección y perfil
- [ ] Un solo grupo de selección: clic al pack especial lo equipa y lo
      desagranda a los colores; clic a un color equipa "clasico" y
      desagranda al especial. Fuente de verdad = `Profile.EquippedPackId`.
- [ ] `EquippedPackId2` / colores por slot para el rival (perfil por jugador).
- [ ] `UGSProfileManager.PerfilListo` evita mostrar una selección
      provisional antes de que llegue la nube; `OnProfileLoaded` refresca.
- [ ] Logs `[Selector] Caja actual...` / `LISTO PARA JUGAR...` para poder
      verificar la elección desde la consola.

### Paso E — Prueba (REGLA DE ORO)
- [ ] **Un paso a la vez** → Play → verificar → commit (lección 17).
- [ ] Con "clasico" **nada cambia de vista**. Si cambia, el paso está mal.
- [ ] Guardar escenas **solo fuera de Play**.
- [ ] Revisar con captura: tamaño, color, capas, rango de movimiento y
      que el rival NO tenga los objetos del pack del jugador 1.
- [ ] Si rompe: `git reset --hard <commit del paso anterior>`.

---

## 2. ERRORES YA COMETIDOS (no repetir)

| Síntoma | Causa | Solución |
|---|---|---|
| Casillas planas de colores | `Pack Id` guardado como pack inexistente | Volver a `clasico` en el Inspector |
| Caldero sin verde | El color vivía solo en el botón del menú | `usaColorPropio` en `FxPack` |
| Todos (incluido el bot) con burbujas/ojos | Pack global `BoardFx.Current` | `PackFor(owner)` por casilla |
| Objetos deformes | Proporciones de canvas aplicadas a mundo | Medir px, convertir, normalizar sprite |
| Objetos diminutos | Falta de normalización por `sprite.bounds` | `escala = diámetro / bounds` |
| Objetos detrás del líquido | Mismo `sortingOrder` y Z ≥ 0 | Z negativo (cámara en -10) |
| Iris diminuta | Doble escalado (hija del ojo) | Dividir por escala del padre |
| Iris azul flotando | Z local muy negativa | Z local `-0.0005` |
| Hueso todo verde | Copia sin recorte | `SpriteMask` con medidas de `ZonaAgua` |
| Todos los respiros se reinician | `SyncAll` reconfigura todo | Frenar con `changed` / ids |
| Error `duration while playing` | Reconfigurar partículas vivas | `Stop(StopEmittingAndClear)` primero |
| Sonido desbalanceado | Clips masterizados distinto | `boomVolume` por pack + perillas |
| "Primero apareció otro color" | UI antes de cargar la nube | `PerfilListo` + `OnProfileLoaded` |

---

## 2b. PROCEDIMIENTO PARA EFECTOS FUTUROS (burbujas/vapor — lecciones finales)

> Como hacer que un efecto UI del menu se vea IGUAL en el juego, paso a
> paso (aplica a cualquier efecto futuro: llamas, chispas, destellos...).

1. **NO copiar numeros UI a world-space**: el menu trabaja en PIXELES del
   RectTransform del Liquido (boton 100px); el juego en UNIDADES (celda
   1.2u). El "rect" equivalente se mide con
   `sprite.bounds.size * abs(localScale)` del SpriteRenderer del liquido.
2. **Fondo del liquido = `-alto*0.5`** (pivote centrado, igual que el menu).
   Nunca usar un numero fijo de celda: hacia que las burbujas salieran
   del cristal.
3. **Misma logica del menu, sin inventar**: tasa `* densidad` (no escalar
   por nivel), fade in 0.12s, crecimiento 0.85→1.15, pop al llegar a la
   superficie, variacion por particula (alpha/mezcla/ritmo).
4. **Doble capa por Z, NO por sortingOrder**: liquidOrder=0 y
   glassOrder=1 son consecutivos (no hay entero entre ellos). Ambas capas
   van en order 0; la de atras en Z~-0.007 (detras de los objetos), la de
   delante en Z~-0.019 (delante de los objetos). El cristal (order 1)
   queda encima siempre.
5. **Tinte sumergido para no verse "pegado al frente"**: el velo del color
   REAL del liquido (leido cada frame) es lo que da la sensacion de estar
   DENTRO, no el Z. Subir el velo en vez de acercar/alejar la capa.
6. **Normalizar sprites generados por codigo**: `localScale = tamano /
   sprite.bounds` (sino el sprite de 64px/PPU100 sale 1.6x grande).
7. **Sprites static mueren al salir del Play**: las listas estaticas de
   sprites generados sobreviven con referencias null. `Elegir()` debe
   detectar nulls, limpiar y regenerar (como `Anillos()`/`Puffs()` del menu).
8. **Limpieza al quedar sin nivel / cambiar de dueno**: liberar TODO al
   pool; nunca hacer `return` dejando particulas visibles (humo congelado).

## 2c. LA CAJA TAMBIÉN DEFINE LOS NÚMEROS
> El blanco→rojo del numero se perdia sobre el liquido verde del caldero.
> Igual que color/efectos/objetos/sonido, el **pack define su paleta de
> numeros**: `numeroPropio`, `numeroColorBajo`, `numeroColorAlto`.
> Caldero = amarillo claro (1) → amarillo explosion (3), con CONTORNO NEGRO
> (`numeroContorno`, `numeroContornoColor`) para que se lea aunque haya
> muchos objetos en nivel 3. Clasico = blanco→rojo VIVO (`numberHigh`
> 1/0.12/0.12) con contorno negro 0.35 que resalta sobre liquido/cristal.
> `NumberColorFor(level, owner)` resuelve por pack del dueno. Fix final: el
> numero es BLANCO PURO en niveles 1 y 2 (antes interpolaba a rosa/ambar en
> el 2 y se camuflaba con el liquido); color de explosion SOLO en el nivel
> maximo (3). Ojo: estos
> colores son campos serializados: si GameScene tiene valores viejos hay
> que actualizarlos a mano en el Inspector (el default no los pisa).

## 2d. COLOR DE LIQUIDO VIVO
> El usuario aclaro que quiere el brillo en los CLASICOS (que ya estaban
> vivos antes). Se subio el boost GLOBAL `BoardFx.ColorBoost` de 1.4 a 1.8,
> que es el que usan todos los colores al tinter el liquido. Se quito el
> `colorBoostPropio` del pack (no hacia falta y daba doble boost al caldero).
> **Causa raíz**: el PNG `liquido animacion.png` exportado es GRIS OSCURO;
> el azul de Photoshop venia de una capa Tono/saturacion (Tono -100) que no
> se aplico al exportar. Unity multiplica gris×color → apagado. Por eso el
> **Solucion final**: subir el boost quemaba el rosa/naranja (se volvian
> blanquecinos). Ahora `BoardFx.AclararLiquido(sprite)` convierte el gris
> oscuro a gris CLARO (0.45..1.0, normalizado, manteniendo olas y alpha) en
> runtime y lo cachea. Se aplica en `CellVisuals.LateUpdate` (juego) y al
> asignarFrames en `ColorSwatch`/`SwatchCaldero` (menu). El boost volvio a
> **1.0** → rosa y naranja son los colores EXACTOS (con 1.4 se volvian
> magenta y amarillo). El sprite aclarado subio a 0.65..1.0 para seguir
> vivo sin distorsionar el tono. Ademas se unifico la seleccion por MODO en
> `ColorSwatch.Update` y `UIManager.UpdateSwatchSelection` (antes peleaban
> y el boton naranja no se agrandaba).

## 2e. SELECCION DE COLOR POR MODO
> El naranja no se podia seleccionar: `UpdateSwatchSelection` comparaba el
> color del botón con `ColP1` con tolerancia 0.02; si el naranja del
> Inspector no era EXACTAMENTE (1,0.45,0.05), nunca se marcaba activo.
> Ahora se compara por **modo** (`ColorSwatch.colorDelJuego`: 0=rosa,
> 1=naranja, 2=custom) contra `Profile.PlayerColorMode`. El botón OnClick
> de cada swatch debe seguir llamando a `UIManager.OnClickPlayerColorMode(
> indice)` para guardar el color en Cloud Save.

## 2f. COLOR BOOST IGUAL EN MENU Y JUEGO
> El usuario mostro que el `Color Boost = 2.3` del Inspector de `CellVisuals`
> es lo que hace vivos los colores en el juego, y lo queria en el menu.
> `SwatchCaldero` gana el campo `colorBoost = 2.3` y en Awake/OnEnable
> setea `BoardFx.ColorBoost` (global), asi el caldero Y los 3 botones de
> color del menu se tinen con el MISMO brillo del juego. **Valor final 1.0**:
> con 2.3 el verde se veia bien pero el rosa/naranja se recortaban a
> magenta/amarillo (sus canales ya estan en 1.0). Para que el verde siga
> vivo con boost 1.0 se subio `colorLiquidoPropio` del caldero a
> (0.1, 0.95, 0.22) y el sprite aclarado a gris 0.8..1.0. Asi rosa=naranja=
> verde son los colores EXACTOS e iguales en menu y juego. **Fix final**: el
> valor guardado en GameScene era `colorBoost: 2` y `CellVisuals.Awake` lo
> pisaba en `BoardFx.ColorBoost` → rosa `(1,0.15,0.6)×2` se recortaba a
> `(1,0.3,1)` = MORADO.
>
> **DECISION FINAL del usuario**: dos intensidades INDEPENDIENTES.
> - **JUEGO**: mantiene su intensidad actual. `CellVisuals.Awake` setea
>   `BoardFx.ColorBoost = colorBoost` (2 de la escena); solo lo usa el juego.
> - **MENU**: NO usa Boost. `ColorSwatch` y `SwatchCaldero` asignan el color
>   directo (`liquidImage.color = color`), asi respira el rosa/naranja/verde
>   natural sin virar a morado. El menu ya NO setea BoardFx.ColorBoost.
>
> **Diferenciacion en el juego**: `FxPack.colorIntensidadJuego = 0.7`
> oscurece un poco el liquido de los packs especiales en
> `BoardFx.LiquidColorFor(owner, pack)`. Los clasicos (usaColorPropio=false)
> mantienen su intensidad y el menu no se ve afectado (no usa esa funcion).

## 2g. BOTONES CLASICOS = COLORES OFICIALES
> Los 2 botones clasicos del menu se veian con tonos raros porque tenian
> `colorDelJuego = -1` (color manual del Inspector). `ColorSwatch.ModoResuelto()`
> ahora infiere el modo del nombre del GameObject ("Rosa"/"Naranja"/
> "Pink"/"Orange") y `ColorResuelto()` usa SIEMPRE `PlayerColorOptions.Get`:
> rosa (1/0.15/0.6) y naranja (1/0.45/0.05) oficiales = IGUAL que el juego.
> Seleccion y tint usan `ModoResuelto()` en `ColorSwatch` y `UIManager`.
> Si los GameObject no se llaman Rosa/Naranja, poner `Color Del Juego` a
> 0 y 1 en el Inspector (es mas directo y evita la inferencia por nombre).

## 3. CAMPO DE APLICACIÓN

Este manual aplica a **cualquier pack futuro**: el esqueleto (catálogo,
elección por perfil, color por dueño, efectos y sonido por pack, objetos
por nivel, espejo por cliente en PvP) ya está hecho. Para un pack nuevo
solo hay que: medir sus objetos en el menú, cargar sus sprites/sonidos,
ajustar su receta en `FxPack` y probar un nivel por vez.
