# Reaction Domino — instrucciones para Claude Code

Juego de reacción en cadena para dos jugadores (Unity 6, UGS). Es un proyecto individual del curso **Cloud 2026**.

## Enfoque del curso

"En este curso se enseñan las bases de multiplayer online (por turnos) usando Claude Code y su sistema de mensajes." Por eso el proyecto **debe terminar con una partida online por turnos entre dos jugadores en dos equipos**. El modo Humano vs. Bot es práctica, no el objetivo. Antes de implementar el online, mira `docs/TAREAS.md` y `docs/MENSAJES.md`.

## Cómo trabajar aquí

- Explica el *porqué* de cada cambio. Es un proyecto de aprendizaje: prefiere guiar al alumno antes que entregar todo hecho, salvo que lo pida.
- Responde en español.
- Trabaja en una rama por tarea y abre un PR. No hagas commits directos a `main`. Mensajes de commit descriptivos.
- **Nunca** subas credenciales, tokens ni claves. Los IDs de proyecto de Unity en `ProjectSettings` no son secretos.
- No subas archivos de recuperación del editor (`Assets/_Recovery/`) ni recursos de más de unos pocos MB sin Git LFS.

## Arquitectura (resumen)

- `Assets/scrips/GameLogic.cs`: **reglas del juego**. Debe seguir siendo una clase pura, sin `MonoBehaviour` ni escenas. Es lo que se llevará al servidor.
- `Assets/scrips/GameManager.cs`: vista y flujo de la partida en `GameScene`. No debe contener reglas.
- `Assets/scrips/UGS*.cs`: servicios de UGS (auth, perfil, Remote Config, Cloud Code).
- `Assets/CloudCode/ValidateMatchResult.js`: endpoint del servidor.

## Reglas de diseño del modo online

1. **El servidor es la autoridad.** El cliente envía intenciones ("pongo un punto en (x, y)") y pinta lo que el servidor responde. Nunca envía quién ganó ni cuántas monedas le tocan.
2. El servidor identifica al jugador por el `playerId` del token, **nunca** por un campo que mande el cliente.
3. Cada jugada lleva `requestId` (idempotencia) y `expectedTurnNumber` (detecta estado viejo).
4. Monedas, gemas, victorias y MMR solo los escribe Cloud Code.
5. La hora límite de cada turno la fija y comprueba el servidor.

## Cosas que NO hay que hacer

- No poner reglas dentro de `GameManager` o de la UI.
- No añadir más tienda, efectos o personalización hasta que exista la partida online (ver `docs/TAREAS.md`).
- No confiar en que "el cliente no hace trampa": un cliente modificado puede llamar a Cloud Code con lo que quiera.

## Ejecutar y probar

Ver `README.md`. Las pruebas (EditMode) todavía no existen; crearlas es la tarea 2 de `docs/TAREAS.md`.
