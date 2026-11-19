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

## 3. CAMPO DE APLICACIÓN

Este manual aplica a **cualquier pack futuro**: el esqueleto (catálogo,
elección por perfil, color por dueño, efectos y sonido por pack, objetos
por nivel, espejo por cliente en PvP) ya está hecho. Para un pack nuevo
solo hay que: medir sus objetos en el menú, cargar sus sprites/sonidos,
ajustar su receta en `FxPack` y probar un nivel por vez.
