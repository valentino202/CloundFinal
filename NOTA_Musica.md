# MUSICA DEL JUEGO

> Musica de fondo con `MusicManager.cs` (commit `90c07bf`).

## Como funciona
- **Un solo AudioSource**, `DontDestroyOnLoad` (sobrevive al cambio de escena).
- **Menu (SampleScene)** → `musicaMenu` (la de "ciencia"), en loop.
- **Pelea (GameScene)** → `musicaPelea` (el rock), en loop.
- **Fin de partida** (ganar/perder/empatar) → `Cortar()` (Stop).
- **Reiniciar / Revancha** → `ReanudarPelea()` (vuelve a empezar).
- **Boton Menu** → `IrAMenu()` (reactiva y suena la del menu).
- **Infinito**: no necesita nada especial, `AudioSource.loop = true` hace
  que la musica vuelta a empezar sola al terminar el clip.

## Setup (lo hace el usuario en el Inspector)
1. En la escena del **menu (SampleScene)** crear GameObject vacio `MusicManager`.
2. Agregar el componente **Music Manager**.
3. Arrastrar:
   - **Musica Menu** → `💡 Música para Videos de Ciencia...mp3`
   - **Musica Pelea** → `lnplusmusic-action-action-rock-613066.mp3`
   - **Volumen** = 0.6 (ajustable)
4. No poner otro en GameScene: el componente se autodetecta por nombre de
   escena y se auto-reproduce.

## Puntos conectados en GameManager
- `ShowResultPanel()` → `MusicManager.Instance.Cortar()`
- Boton "Reiniciar" y "Revancha" → `ReanudarPelea()`
- Botones "Menu" (ambos) → `IrAMenu()` antes de `LoadScene("SampleScene")`

## Nota
Los clips entraron al commit (estaban sin track). No se toco ningun otro
audio (boom, burbujas, vapor siguen igual).

## BUILD (ejecutable) — importante
- Build Settings: SampleScene + GameScene habilitadas ✅
- `UnityEditor` solo dentro de `#if UNITY_EDITOR` → compila ✅
- **Los clips de musica van en el GameObject MusicManager del Inspector**,
  asi que se incluyen en el build automaticamente ✅ (no hace falta Resources).
- **Recursos del pack caldero**: `FxPacks.Cargar<T>()` usa `Resources.Load`
  (funciona en build). Para que el caldero tenga hongos/ojo/hueso/sonido en
  el ejecutable, **copiar estos archivos a `Assets/Resources/`** (yo cree la
  carpeta, pero no muevo binarios):
  `explosion_caldero.mp3`, `hongo_color1.png`, `Hongo_color2.png`,
  `base_ojo_color.png`, `ojo_iris_central/lateral/vertical.png`, `Huesito.png`.
  Sin esto el Editor sigue funcionando (fallback por AssetDatabase), pero el
  build se veria sin esos objetos. Version del producto: `1.0` (ProjectSettings).
