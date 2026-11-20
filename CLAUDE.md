# Reaction Domino — instrucciones para Claude Code

Juego de reacción en cadena para dos jugadores (Unity 6, UGS). Es un proyecto individual del curso **Cloud 2026**.

## Enfoque del curso

"En este curso se enseñan las bases de multiplayer online (por turnos) usando Claude Code y su sistema de mensajes." Por eso el proyecto **debe terminar con una partida online por turnos entre dos jugadores en dos equipos**. El modo Humano vs. Bot es práctica, no el objetivo. Antes de implementar el online, mira `docs/TAREAS.md` y `docs/MENSAJES.md`.

**Qué es "el sistema de mensajes":** los **mensajes push de Cloud Code** (Wire). Cuando el servidor guarda una jugada, avisa al rival con un mensaje liviano `{type, matchId}`; el rival no confía en el mensaje, solo lo usa como señal para volver a pedirle el estado al servidor. Un sondeo cada ~10 s queda solo como respaldo.

El proyecto sigue la misma arquitectura que el proyecto de referencia del profesor (TTTXO): reglas en C# puro compartidas con un módulo de Cloud Code, funciones autoritativas `CreateMatch` / `PlayMove` / `GetMatchState`, estado escrito solo por el servidor y aviso al rival por push.

## Cómo trabajar aquí

- Explica el *porqué* de cada cambio. Es un proyecto de aprendizaje: prefiere guiar al alumno antes que entregar todo hecho, salvo que lo pida.
- Responde en español.
- Trabaja en una rama por tarea y abre un PR. No hagas commits directos a `main`. Mensajes de commit descriptivos.
- **Nunca** subas credenciales, tokens ni claves. Los IDs de proyecto de Unity en `ProjectSettings` no son secretos.
- No subas archivos de recuperación del editor (`Assets/_Recovery/`) ni recursos de más de unos pocos MB sin Git LFS.

## Arquitectura (resumen)

- `Assets/scrips/GameLogic.cs`: **reglas del juego** (la capa *Core*). Debe ser C# puro: sin `MonoBehaviour`, sin escenas y **sin `UnityEngine`**, para que el módulo de Cloud Code pueda compilar esos mismos archivos y servidor y cliente ejecuten el mismo código.
- `Assets/scrips/GameManager.cs`: vista y flujo de la partida en `GameScene`. No debe contener reglas.
- `Assets/scrips/UGS*.cs`: servicios de UGS (auth, perfil, Remote Config, Cloud Code).
- `Assets/CloudCode/ValidateMatchResult.js`: endpoint actual, **provisional**. Se reemplaza por la liquidación de premios dentro de `PlayMove` (ver `docs/TAREAS.md`).
- Módulo de Cloud Code (futuro): en una carpeta que Unity ignore (nombre terminado en `~`), compilando los archivos del Core con `<Compile Include>`.

## Reglas de diseño del modo online

1. **El servidor es la autoridad.** El cliente envía intenciones ("pongo un punto en (x, y)") y pinta lo que el servidor responde. Nunca envía quién ganó ni cuántas monedas le tocan.
2. El servidor identifica al jugador por el `playerId` del token (`context.PlayerId`), **nunca** por un campo que mande el cliente. Debe comprobar que es jugador de esa partida y que es su turno.
3. **El estado de la partida es la lista de jugadas** (solo se agregan), guardada en Cloud Save. En cada llamada el servidor la **reproduce** con un `GameLogic` nuevo para obtener tablero, turno y resultado. Funciona porque las reglas son deterministas. Al cliente se le devuelve una **vista** (DTO), distinta del registro interno.
4. **Mensajes:** al guardar una jugada, push al rival con payload liviano (`{type, matchId}`, sin estado). El cliente vuelve a pedir el estado con `GetMatchState`. Sondeo de respaldo de ~10 s. Que falle el push no puede hacer fallar la jugada.
5. **Idempotencia:** `CreateMatch` devuelve la misma partida si se llama dos veces, y los premios se liquidan **una sola vez**, al terminar la partida (una jugada repetida sobre una partida terminada se rechaza).
6. **Cloud Save, clases de acceso:** lo que decide el servidor (monedas, gemas, victorias, MMR, estado de la partida) va en **Protected** o **Private**, **nunca en Default**, porque Default lo puede escribir el propio jugador con su token. Solo las preferencias (color, pack equipado) pueden ir en Default. Escribe un comentario `ACCESS CLASS` junto a cada escritura para poder buscarlo.
7. **Configuración compartida:** el servidor fija las reglas de la partida (tamaño, nivel de explosión, tope de turnos, tiempo de turno) al crearla, y el cliente las lee de ahí, no de Remote Config por su cuenta.
8. **Plazos y abandono** con el reloj del servidor, nunca el del cliente. Se resuelven al consultar el estado (`GetMatchState`): si el plazo del turno venció o el rival lleva demasiado sin actividad, el servidor resuelve la partida.
9. Una **versión de protocolo** compartida entre cliente y servidor, que se sube en ambos cuando cambia el contrato de una función.

## Cosas que NO hay que hacer

- No poner reglas dentro de `GameManager` o de la UI.
- No escribir las reglas dos veces (por ejemplo, otra vez en JavaScript): se reutiliza el mismo código C#.
- No mandar el estado dentro del push.
- No añadir más tienda, efectos o personalización hasta que exista la partida online (ver `docs/TAREAS.md`).
- No confiar en que "el cliente no hace trampa": un cliente modificado puede llamar a Cloud Code con lo que quiera.

## Ejecutar y probar

Ver `README.md`. Las pruebas (EditMode) todavía no existen; crearlas es la tarea 2 de `docs/TAREAS.md`.
