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
