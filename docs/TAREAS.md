# Tareas para llegar a una partida online por turnos

Este documento lo escribió el profesor (con ayuda de Claude Code) tras revisar el repo. **No hay código nuevo del juego en este PR**: las tareas son tuyas. Cada una explica qué hacer y **por qué**, y te da una pista, no la solución. Hazlas en orden, con una rama y un PR por tarea.

> Objetivo del curso: dos jugadores, en dos equipos distintos, juegan una partida completa de Reaction Domino por turnos, y **el servidor decide** qué jugadas son válidas y quién gana.

## Por qué tu juego es un buen primer online

Reaction Domino es de **información perfecta y sin azar**: todo el tablero es público y cada jugada tiene un resultado determinista. No hay fichas ocultas. Entonces el servidor solo tiene que garantizar tres cosas: **quién juega y cuándo**, **que la jugada sea legal** y **que el resultado sea cierto**. Como además es determinista, el servidor puede reproducir la partida jugada a jugada sin creerle a nadie.

---

## Tarea 1 — Diseñar el protocolo en papel (antes de programar)

**Qué:** rellena [`MENSAJES.md`](MENSAJES.md): la tabla de mensajes, qué guarda el servidor y un diagrama de secuencia de **un turno completo**, incluido el caso "no me llegó la respuesta y reintento".

**Por qué:** si no sabes qué mensajes existen, qué contienen y quién los valida, el código sale a ciegas. Es la tarea más corta y la que más se aprende.

**Pista:** piensa en `CreateMatch`, `JoinMatch`, `PlacePoint`, `GetMatch` y quizá `Forfeit`. ¿Qué campos son indispensables? ¿Cuáles **no** debe enviar nunca el cliente (por ejemplo, "quién soy" o "quién ganó")?

**Listo cuando:** el profesor revisó `MENSAJES.md`.

## Tarea 2 — Sacar las reglas de Unity y ponerles pruebas

**Qué:**
- Que `GameLogic` compile **sin** `UnityEngine` (hoy usa `Vector2Int` y `Mathf`). Un módulo de Cloud Code no tiene Unity.
- Crear pruebas EditMode para: jugada fuera de turno, celda rival, índice fuera de rango, explosión simple, explosión en cadena, ganador por eliminación (solo cuando ambos jugaron) y ganador por celdas al llegar a `maxTurns`.

**Por qué:** las reglas son lo que el servidor va a ejecutar. Si no están probadas, un error ahí se convierte en trampa o en partidas rotas para los dos jugadores. Tu propio comentario en `MatchController` dice que la regla es "testeable sin escena": ¡aprovéchalo!

**Pistas:**
- Un `struct` propio con `x` e `y` y `System.Math.Max` reemplazan lo de Unity. Ojo: `GameManager`, `CellVisuals` y `BoardFx` usan los resultados de `GameLogic`; hay que actualizarlos a la vez.
- Para que las pruebas EditMode vean tus scripts normalmente hace falta una *Assembly Definition* para `Assets/scrips`. Pídele a Claude Code que te explique el porqué antes de crearla.
- Añade también un `turnNumber` y devuelve los errores como un código (`outcome`) además del texto.

**Listo cuando:** las pruebas pasan en *Window → General → Test Runner*.

## Tarea 3 — Cerrar el hueco de los premios (aunque aún no haya online)

**Qué:** hoy se pueden conseguir monedas, victorias y MMR sin jugar. Revisa y corrige:
- `ValidateMatchResult` acepta lo que diga el cliente y no tiene identificador de partida: se puede llamar a mano varias veces.
- El cliente escribe `Coins`, `Gems`, `TotalWins` y `TotalMatches` en Cloud Save (`UGSProfileManager.SaveProfileAsync`).
- **Posible bug (compruébalo en el editor):** al terminar la partida, `RecordMatchResultAsync` guarda el perfil local entero con saldos antiguos y puede **sobrescribir** lo que sumó el servidor.
- Un empate se manda como victoria con celdas iguales y el servidor lo rechaza (`winnerCells <= loserCells`).
- Comprueba que `cloudSave.getItems(...)` en Cloud Code devuelve lo que el código espera (`saved?.Gems`).

**Por qué:** es la lección central del curso: **nunca confíes en el cliente**. Un cliente modificado puede mandar cualquier cosa.

**Pista:** pregúntate "¿quién escribe cada clave y desde dónde?". Las claves que dan ventaja (monedas, victorias, MMR) deben escribirlas solo el servidor; las preferencias (color, pack equipado) pueden seguir en el cliente.

**Listo cuando:** puedes explicar con tus palabras por qué llamar a Cloud Code "a mano" ya no da premios.

## Tarea 4 — Partida online en el servidor

**Qué:** crear en Cloud Code la partida con `CreateMatch`, `JoinMatch`, `PlacePoint` y `GetMatch`. El estado se guarda en Cloud Save (datos privados de la partida) con el código de partida como clave y control de escrituras simultáneas. El servidor valida en este orden: `requestId` repetido → ¿eres de esta partida? → ¿está en curso? → `expectedTurnNumber` → ¿te toca? → ¿jugada legal? → aplicar y guardar.

**Por qué:** aquí se vuelve real el concepto de autoridad. El curso trae un ejemplo de este patrón en `Hellscythe25/Cloud2026` (PoC `TurnMatch`): léelo y compáralo con tu diseño.

**Pista:** como tu motor es C# puro, puedes reutilizarlo en un módulo C# de Cloud Code en vez de reescribirlo en JavaScript.

**Listo cuando:** dos clientes (dos editores, o editor + build) juegan una partida completa y un cliente que manda una jugada ilegal o fuera de turno recibe un rechazo claro sin cambiar el estado.

## Tarea 5 — Cliente online

**Qué:** un servicio de cliente (por ejemplo `OnlineMatchService`) que envía la intención y pinta lo que devuelve el servidor. El rival se descubre por **sondeo** (por ejemplo cada 2 s con `GetMatch`). Guarda el `requestId` pendiente para poder reintentar. Mantén el bot como modo "Práctica".

**Por qué:** `GameManager` hoy llama directo a `GameLogic`. En online la verdad vive en el servidor y la pantalla solo la refleja.

**Pista:** la lista ordenada de explosiones que devuelve el servidor es justo lo que ya consume `ExplosionSequence`. ¿Qué haces si llega un estado nuevo mientras el anterior aún se anima?

**Listo cuando:** cerrar un cliente a mitad de partida y volver con el mismo código restaura el estado.

## Tarea 6 — Tiempo por turno, abandono y reconexión

**Qué:** una hora límite por turno fijada **por el servidor** (`turnDeadlineUtc`), qué pasa cuando se agota (pierde el turno / pierde la partida tras N veces) y `Forfeit`.

**Por qué:** si un jugador se desconecta, el otro no puede quedarse esperando para siempre.

---

## Higiene del repositorio (puedes hacerlo en paralelo)

- [ ] **Recursos pesados:** `Assets/imagenes/efectos.png` (30 MB), `titulo 1.png` (29 MB) y `titulo normal.png` (11 MB). GitHub rechaza archivos de 100 MB. Reduce su tamaño o investiga Git LFS (añadir `.gitattributes` **no** mueve los que ya están subidos).
- [ ] **`MAX_TURNS`:** se descarga de Remote Config pero `GameManager` usa un campo del Inspector. Decide cuál manda.
- [ ] **Paquetes sin usar** (`economy`, `multiplayer.center`, `visualscripting`, `timeline`, `2d.tilemap.extras`…): quita los que no necesites.
- [ ] **Faltas en nombres:** carpetas `scrips`, `animetion`, nombre del repo `CloundFinal`. Renombra en Unity **dentro del editor** para no romper las referencias.
- [ ] **Configuración de UGS:** versiónala (valores de Remote Config, leaderboard) o documéntala en el README para que otra persona pueda reproducir el entorno.

## Cómo pedírselo a Claude Code (sugerencia)

Empieza cada tarea pidiendo primero una **explicación** ("¿por qué hace falta X?, ¿qué opciones hay?") y deja que te proponga un plan antes de editar. Lee el diff antes de aceptarlo. Si no puedes explicar un cambio con tus palabras, aún no lo has entendido.
