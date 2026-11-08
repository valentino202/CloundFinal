# Tareas para llegar a una partida online por turnos

Este documento lo escribió el profesor (con ayuda de Claude Code) tras revisar el repo. **No hay código nuevo del juego en este PR**: las tareas son tuyas. Cada una explica qué hacer y **por qué**, y te da una pista, no la solución. Hazlas en orden, con una rama y un PR por tarea.

> Objetivo del curso: dos jugadores, en dos equipos distintos, juegan una partida completa de Reaction Domino por turnos. **El servidor decide** qué jugadas son válidas y quién gana, y **avisa al rival con un mensaje push** de Cloud Code cuando hay una jugada nueva.

## Estado actual (actualizado con el PR #2 del profesor)

El profesor adelantó la **base técnica** para que tengas algo concreto que leer, probar y ampliar. **Tu trabajo ahora no es copiarla, sino entenderla**: léela, ejecútala, rómpela a propósito y explícala con tus palabras. Las pruebas de cada parte están en [`PRUEBAS.md`](PRUEBAS.md).

| Tarea | Estado | Qué te toca a ti |
|---|---|---|
| 1. Protocolo en papel | **Tuya, pendiente** | Rellenar `MENSAJES.md` y compararlo con lo que hizo el PR #2. Si tu diseño difiere, discútelo con el profesor. |
| 2. Core sin Unity + pruebas | **Hecha en el PR #2** (37 pruebas) | Entender por qué `GameLogic` ya no usa `UnityEngine`, qué es `MatchReplay` y por qué `PlacePoint` devuelve un `MoveOutcome`. **Añade al menos 3 pruebas nuevas** de reglas que se te ocurran. |
| 3. Parar la hemorragia de premios | **Tuya, pendiente** | El PR #2 **no** toca `ValidateMatchResult.js` ni `UGSProfileManager`. Sigue pudiendo abusarse. |
| 4. Partida en el servidor | **Base hecha en el PR #2** (`GetGameConfig`, `CreateMatch`, `PlayMove`, `GetMatchState`, push, premios en Protected) | Desplegarlo, probarlo (ver `PRUEBAS.md`), leer la config de Remote Config, y entender cada comprobación de `PlayMove`. Lo que **no** está hecho: ver la lista al final de la Tarea 4. |
| 5. Cliente online con push | **Servicio hecho en el PR #3** (`OnlineMatchService`, DTOs y un arnés de pruebas); **la pantalla es tuya** | El juego todavía no usa el servicio: falta la pantalla de partida online (lobby con código, tablero que pinta el estado del servidor, bloqueo cuando no es tu turno, animar `lastMoveExplosions`) y leer el saldo de Protected. Ver la lista al final de la Tarea 5. |
| 6. Plazos y abandono | **Tuya, pendiente** | El módulo todavía no tiene plazo por turno ni abandono. |

## La idea central

**El cliente nunca decide nada.** El cliente envía intenciones ("pongo un punto en (x, y)"). El servidor (Cloud Code) las valida con las **mismas reglas del juego**, guarda el resultado y avisa al rival con un push. El rival no confía en el contenido del mensaje: solo lo usa como señal para volver a pedirle el estado al servidor.

```
Cliente A ──CreateMatch──▶ Cloud Code ──▶ Cloud Save (registro de la partida)
Cliente A ──PlayMove(matchId, x, y)──▶ Cloud Code: valida turno + reglas → guarda
                                         └─ push {type, matchId} ──▶ Cliente B
Cliente B ──GetMatchState(matchId)──▶ Cloud Code ──▶ vista del estado para B
```

Tu juego es de **información perfecta y sin azar**: todo el tablero es público y cada jugada tiene un resultado determinista. Eso lo hace un primer online muy limpio: el servidor puede **reproducir** la lista de jugadas con el mismo motor y obtener el estado, sin creerle a nadie.

---

## Tarea 1 — Diseñar el protocolo en papel (antes de programar)

**Qué:** rellena [`MENSAJES.md`](MENSAJES.md): las funciones, el mensaje push, el estado, la clase de acceso de cada dato, los plazos y un diagrama de secuencia de **un turno completo con push**.

**Por qué:** si no sabes qué mensajes existen, qué contienen y quién los valida, el código sale a ciegas. Es la tarea más corta y la que más se aprende.

**Pista:** pregúntate "¿por qué el push no lleva el estado?" y "¿qué pasa si el push se pierde?". Y qué datos **no** debe enviar nunca el cliente (por ejemplo "quién soy" o "quién ganó").

**Listo cuando:** el profesor revisó `MENSAJES.md`.

## Tarea 2 — Una capa *Core* sin Unity, con pruebas

**Qué:**
- Separar las reglas (`GameLogic` y lo que necesite) en una capa **Core** que compile **sin `UnityEngine`** (hoy usa `Vector2Int` y `Mathf`), con su propia *Assembly Definition* sin referencias a Unity. Así luego el módulo de Cloud Code podrá compilar **esos mismos archivos** y servidor y cliente ejecutarán exactamente el mismo código.
- Que el motor se pueda **reproducir**: un `GameLogic` nuevo + una lista de jugadas `(x, y)` = siempre el mismo estado. Y que el jugador que mueve se deduzca del orden de las jugadas, no de un argumento que mande el cliente.
- Pruebas EditMode: jugada fuera de turno, celda rival, índice fuera de rango, explosión simple, explosión en cadena, ganador por eliminación (solo cuando ambos jugaron), ganador por celdas al llegar a `maxTurns`, y que **reproducir una lista de jugadas da el mismo estado**.

**Por qué:** las reglas son lo que el servidor va a ejecutar. Si no están probadas, un error ahí se convierte en trampa o en partidas rotas para los dos jugadores. Y si el servidor tiene su propia copia de las reglas, tarde o temprano las dos copias se desincronizan.

**Pistas:**
- Un `struct` propio con `x` e `y` y `System.Math.Max` reemplazan lo de Unity. `GameManager`, `CellVisuals` y `BoardFx` usan los resultados de `GameLogic`; hay que actualizarlos a la vez.
- Pídele a Claude Code que te explique **por qué** hace falta una *Assembly Definition* para que las pruebas vean tus scripts.
- Devuelve los errores como un código además del texto, para que el cliente sepa qué mostrar.

**Listo cuando:** las pruebas pasan en *Window → General → Test Runner*.

## Tarea 3 — Parar la hemorragia de premios (arreglo provisional)

**Qué:** hoy se pueden conseguir monedas, victorias y MMR sin jugar. Esto **no se arregla de verdad aquí**, porque la solución definitiva es la Tarea 4 (los premios los liquida el servidor al terminar la partida). Mientras tanto haz lo mínimo:
- No des premios cuando el rival es el bot (o límitalos), porque `ValidateMatchResult` se fía de lo que diga el cliente.
- Comprueba en el editor el **posible bug** de que `RecordMatchResultAsync` guarda el perfil local entero con saldos antiguos y **sobrescribe** lo que sumó el servidor.
- Comprueba que `cloudSave.getItems(...)` en Cloud Code devuelve lo que el código espera (`saved?.Gems`).
- Anota qué hace hoy un empate (el cliente lo manda como victoria con celdas iguales y el servidor lo rechaza).
- **Averigua en qué clase de acceso de Cloud Save cae cada cosa** (ver el recuadro de abajo).

**Por qué:** es la lección central del curso: **nunca confíes en el cliente**. Un cliente modificado puede llamar a Cloud Code con lo que quiera.

> **Clases de acceso de Cloud Save.** Lo que el cliente guarda con `Data.Player.SaveAsync` cae en **Default**, y el propio jugador puede escribir en Default con su token por la API REST, sin pasar por tu juego. Lo que decide el servidor (monedas, gemas, victorias, MMR) debe ir en **Protected** (el jugador lo lee pero no lo escribe) o en **Private** (ni lo lee). Solo las preferencias (color, pack equipado) pueden quedarse en Default. Si el premio cae en Default, todos los controles del servidor son decoración.

**Listo cuando:** puedes explicar con tus palabras en qué clase de acceso está cada dato y por qué.

## Tarea 4 — Partida online en el servidor (Cloud Code)

**Qué:** crear un módulo de Cloud Code con estas funciones:
- `GetGameConfig`: entrega tamaño del tablero, nivel de explosión, tope de turnos y tiempo de turno. El cliente deja de leer Remote Config por su cuenta, para que **los dos jugadores y el servidor usen siempre las mismas reglas**.
- `CreateMatch`: crea la partida. Debe ser **idempotente** (si se llama dos veces, devuelve la misma partida) y decidir los asientos sin que el cliente elija.
- `PlayMove(matchId, x, y)`: valida **en este orden**: cargar la partida → ¿ya terminó? → ¿quien llama (`context.PlayerId`, nunca un id enviado por el cliente) es jugador de la partida? → ¿es su turno? → ¿la jugada es válida según el motor? → añadir la jugada y guardar → si la partida terminó, **liquidar los premios una sola vez** → avisar al rival por push → devolver la vista.
- `GetMatchState(matchId)`: devuelve la vista actual. Sirve para reconectar y para resolver plazos y abandono (Tarea 6).

**Cómo se guarda:** como **lista de jugadas, solo de agregar**, en Cloud Save (datos de la partida, con clave `matchId`), no como foto del tablero. En cada llamada se **reproduce** la lista con un `GameLogic` nuevo. Al cliente se le devuelve una **vista** distinta del registro interno.

**Premios (reemplazan a `ValidateMatchResult`):** los calcula el servidor al terminar la partida, una sola vez (un `PlayMove` repetido sobre una partida terminada se rechaza), y los guarda en **Protected**. El cliente deja de escribir monedas, gemas y victorias; solo las **lee**.

**Por qué:** aquí se vuelve real el concepto de autoridad. La idempotencia de `CreateMatch` y de los premios es lo que permite reintentar sin romper nada.

**Pistas:**
- El módulo va en una carpeta que Unity ignore (un nombre terminado en `~`) y **compila los archivos del Core** con `<Compile Include>`. No reescribas las reglas en JavaScript.
- Añade un número de **versión de protocolo** en cliente y servidor, y súbelo en los dos cuando cambie una función.
- Las preferencias (color, pack) pueden seguir en Default; las monedas, no.

**Listo cuando:** dos clientes (dos editores, o editor + build) juegan una partida completa. Una jugada fuera de turno, con celda rival o con otro jugador es rechazada con un motivo claro sin cambiar el estado. Llamar a Cloud Code "a mano" no da premios.

**Lo que ya hizo el PR #2 y lo que NO** (para que sepas qué te falta):
- ✅ `GetGameConfig`, `CreateMatch` (por código de partida, idempotente), `PlayMove`, `GetMatchState`; estado como lista de jugadas con *replay*; push `match_updated`; premios liquidados una sola vez en **Protected** (clave `onlineProfile`); `GameProtocol.Version`.
- ❌ Leer la configuración de Remote Config en `GetGameConfig` (hoy son constantes en `GameProtocol.cs`).
- ❌ Matchmaker (hoy se empareja con un código de partida compartido a mano).
- ❌ Tope diario / anti-colusión de premios: hoy dos cuentas tuyas podrían jugar entre sí y farmear monedas. Piensa cómo evitarlo (por ejemplo, un contador por rival y por día).
- ❌ MMR / leaderboard `ranked_mmr` desde el servidor.
- ❌ Retirar `ValidateMatchResult.js` y migrar el cliente (`UGSProfileManager`, `UGSEconomy`) para que **lea** `onlineProfile` en vez de escribir `Coins`/`Gems`.
- ❌ Plazo por turno y abandono (Tarea 6).
- ❌ Probado contra UGS real: el PR #2 se compiló y se probó con `dotnet`, pero **nadie lo ha ejecutado aún contra el servicio**. Tus pruebas de `PRUEBAS.md` son las primeras.

## Tarea 5 — Cliente online con push

**Qué:** un servicio de cliente (por ejemplo `OnlineMatchService`) que:
- envía `PlayMove` con la intención y pinta lo que devuelve el servidor;
- se **suscribe a los mensajes push** de Cloud Code (`CloudCodeService.Instance.SubscribeToPlayerMessagesAsync`) y, al recibir el aviso, pide `GetMatchState`;
- tiene un **sondeo de respaldo** (alrededor de 10 s) por si se pierde un push.
Mantén el bot como modo "Práctica".

**Por qué:** `GameManager` hoy llama directo a `GameLogic`. En online la verdad vive en el servidor y la pantalla solo la refleja. El push es el "sistema de mensajes" del curso: avisa al instante, pero **no lleva el estado**, así que nadie puede forjar un mensaje para ganar algo.

**Pista:** la lista ordenada de explosiones que devuelve el servidor es justo lo que ya consume `ExplosionSequence`. ¿Qué haces si llega un estado nuevo mientras el anterior aún se anima? Y recuerda: que falle el push nunca debe hacer fallar la jugada, que ya quedó guardada.

**Listo cuando:** los dos clientes se actualizan por push (y el sondeo de respaldo sigue funcionando si lo desactivas). Cerrar un cliente y volver restaura el estado con `GetMatchState`.

**Lo que ya hizo el PR #3 y lo que NO** (para que sepas qué te falta):
- ✅ `Assets/scrips/Online/OnlineMatchService.cs`: llamadas al módulo (`GetGameConfigAsync` con comprobación de versión del protocolo, `CreateMatchAsync`, `PlayMoveAsync`, `GetMatchStateAsync`), suscripción al push (`StartListeningAsync`) y sondeo de respaldo (`StartPolling`). Los dos avisos terminan en un único evento `MatchUpdated`, que llega en el hilo principal.
- ✅ `OnlineDtos.cs`: los contratos del cliente. Hay pruebas (`ClientContractTests`) que comprueban que lo que escribe el servidor lo puede leer el cliente.
- ✅ `OnlineDebugHarness.cs`: herramienta de pruebas (no es el juego) para ejecutar cada mensaje desde el editor.
- ❌ **La pantalla online.** `GameManager` sigue siendo contra el bot. Piensa si conviene un `OnlineGameManager` aparte o parametrizar el actual: ¿qué parte de `GameManager` es "regla" y cuál es "vista"?
- ❌ Lobby: pedir el código, esperar al rival (`phase = "waiting"`), mostrar el asiento.
- ❌ Tablero que **solo pinta** el estado del servidor (`owner`/`level`) y llama a `PlayMoveAsync` en el clic; qué hacer con un `outcome` distinto de `"ok"`.
- ❌ Animar la cadena con `lastMoveExplosions` (ya tienes `ExplosionSequence`). Pregunta de diseño: si llega un estado nuevo mientras animas el anterior, ¿qué haces?
- ❌ Leer el saldo (`onlineProfile`, **Protected**) y dejar de escribir `Coins`/`Gems` desde el cliente (Tarea 3). Hoy **no existe ninguna llamada que lea ese item**: tendrás que decidir si añades una función `GetOnlineProfile` al módulo o lo lees del Cloud Save con la API de lectura de datos protegidos.

## Tarea 6 — Plazos de turno y abandono, con el reloj del servidor

**Qué:** el tiempo lo mide el servidor, nunca el cliente, y se resuelve **al consultar el estado** (`GetMatchState` y `PlayMove`), sin un temporizador por partida:
- guardar la hora en que empezó cada turno (se reinicia en cada jugada);
- si alguien consulta y el plazo del turno venció, el servidor resuelve la partida como derrota para quien tenía el turno;
- si el rival lleva más de N minutos sin actividad, quien consulta gana por abandono (N viene de configuración, no escrito en el código);
- *(opcional, al final)* un trigger programado que cierre partidas abandonadas por los dos.

**Por qué:** si un jugador se desconecta, el otro no puede quedarse esperando para siempre, y el reloj del cliente se puede manipular.

**Pista:** con push + sondeo de ~10 s, un turno de 2–5 s no es viable. Piensa en 20–30 s.

---

## Higiene del repositorio (puedes hacerlo en paralelo)

- [ ] **Recursos pesados:** `Assets/imagenes/efectos.png` (30 MB), `titulo 1.png` (29 MB) y `titulo normal.png` (11 MB). GitHub rechaza archivos de 100 MB. Reduce su tamaño o investiga Git LFS (añadir `.gitattributes` **no** mueve los que ya están subidos).
- [ ] **`MAX_TURNS`:** se descarga de Remote Config pero `GameManager` usa un campo del Inspector. Con `GetGameConfig` (Tarea 4) lo decide el servidor.
- [ ] **Paquetes sin usar** (`economy`, `multiplayer.center`, `visualscripting`, `timeline`, `2d.tilemap.extras`…): quita los que no necesites.
- [ ] **Faltas en nombres:** carpetas `scrips`, `animetion`, nombre del repo `CloundFinal`. Renombra en Unity **dentro del editor** para no romper las referencias.
- [ ] **Configuración de UGS versionada en el repo** (Cloud Code, Remote Config, leaderboard) o documentada en el README, para que otra persona pueda reproducir el entorno.

## Cómo pedírselo a Claude Code (sugerencia)

Empieza cada tarea pidiendo primero una **explicación** ("¿por qué hace falta X?, ¿qué opciones hay?") y deja que te proponga un plan antes de editar. Lee el diff antes de aceptarlo. Si no puedes explicar un cambio con tus palabras, aún no lo has entendido.
