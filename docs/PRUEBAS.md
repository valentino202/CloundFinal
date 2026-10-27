# Actividades de testing

Estas pruebas son **tuyas**: el profesor y Claude Code hicieron los cambios, pero **nadie los ha jugado en el editor ni ejecutado contra UGS**. Lo verificado hasta ahora:
- Con `dotnet`: el Core compila **sin Unity** y pasa sus 37 pruebas; el módulo de Cloud Code compila y pasa sus 11 pruebas (incluido el contrato con los DTOs del cliente).
- Con **Unity 6000.3.23f1 en modo batch** (una copia del proyecto, sin abrir el editor): el proyecto completo **compila sin errores** (incluidas las *Assembly Definitions* y los cambios en `GameManager`/`MatchController`) y las **37 pruebas EditMode pasan**. Tu proyecto pide la 6000.3.24f1; no estaba instalada.

Tu retroalimentación es la que cierra el ciclo: P1, P2 y P4 siguen siendo tuyas porque quien las corrió no jugó ni abrió el editor con interfaz.

**Cómo reportar:** responde en el PR (un comentario por actividad) con este formato:

```
Actividad: P3
Resultado: ✅ pasó / ❌ falló / ⚠️ pasó con dudas
Lo que vi: (texto corto o captura)
Si falló: mensaje exacto de la consola (copia y pega), qué hiciste justo antes y tu versión de Unity.
```

Si algo falla, **no lo arregles a ciegas**: primero intenta explicar por qué falló y qué parte del código lo causa. Pídele a Claude Code que te ayude a entenderlo, no solo a taparlo.

Versión de Unity del proyecto: `6000.3.24f1`. Para las pruebas con `dotnet` necesitas el SDK de .NET 8 o superior (`dotnet --version`).

---

## Bloque A — Que no se rompió nada (PR #1 y PR #2)

### P1 · El proyecto abre y compila
- **Qué probar:** que Unity importa el proyecto sin errores después de mover `GameLogic` a `Assets/Core` y crear las *Assembly Definitions*.
- **Pasos:** 1) cierra Unity, 2) trae la rama, 3) ábrela con Unity Hub, 4) espera la importación, 5) mira la pestaña *Console*.
- **Resultado esperado:** **cero errores en rojo.** Aparecen archivos nuevos en `Assets/Core`, `Assets/Tests` y `Assets/CloudCode/ReactionDominoModule.ccmr`. Ya no existe `Assets/_Recovery`.
- **Reporta si falla:** el primer error en rojo completo (con archivo y línea). Los errores `CS0246`/`CS0234` de `GameLogic`, `GridPoint` o `ReactionDomino.Core` indicarían que algún script no ve el Core; dime en qué archivo.

### P2 · El juego contra el bot sigue igual
- **Qué probar:** que el cambio del Core no rompió el juego de siempre (`GameManager` ahora convierte `GridPoint` a `Vector2Int` en las explosiones).
- **Pasos:** 1) abre `SampleScene` y dale Play, 2) entra como invitado, 3) juega una partida completa contra el bot hasta que alguien gane, 4) pulsa *Revancha*, 5) juega otra, 6) prueba *Reiniciar* a mitad de partida.
- **Resultado esperado:** las celdas se llenan y **explotan con animación** (también las cadenas), el turno pasa al bot, aparece el panel de resultado, *Revancha* y *Reiniciar* dejan el tablero vacío y **nada nuevo en rojo en la consola**.
- **Reporta si falla:** en qué momento (¿explosión? ¿fin de partida? ¿reinicio?) y el error de la consola. Una explosión que no se anima o una celda que no cambia de color es una regresión del cambio de `GridPoint`.

### P3 · El Core no admite Unity (experimento)
- **Qué probar:** entender por qué `ReactionDomino.Core` es "puro".
- **Pasos:** 1) abre `Assets/Core/GameLogic.cs`, 2) añade al principio `using UnityEngine;` y en algún sitio `var v = new Vector2Int(1, 1);`, 3) guarda y mira la consola, 4) **deshaz el cambio** (no lo subas).
- **Resultado esperado:** error de compilación, porque la *Assembly Definition* del Core tiene `noEngineReferences`.
- **Reporta:** en una frase, **por qué** esa restricción es lo que permite que el servidor use el mismo archivo.

---

## Bloque B — Pruebas automáticas

### P4 · Las 37 pruebas del Core en Unity
- **Pasos:** *Window → General → Test Runner → EditMode → Run All*.
- **Resultado esperado:** **37 en verde** (`GameLogicTests`, `MatchReplayTests`, `RewardRulesTests`).
- **Reporta si falla:** el nombre de la prueba y su mensaje. Si no aparece ninguna, revisa que `Assets/Tests/EditMode/ReactionDomino.Core.Tests.asmdef` exista y que el paquete *Test Framework* esté instalado.
- **Para aprender:** elige una prueba y explica con tus palabras qué regla protege. Después **añade al menos 3 pruebas nuevas** (por ejemplo: un tablero de otro tamaño, una explosión en una esquina, tope de turnos con cadena).

### P5 · Las 7 pruebas del módulo con `dotnet`
- **Pasos:** en una terminal:
  ```bash
  cd CloudCode~/ReactionDominoModule.Tests
  dotnet test
  ```
- **Resultado esperado:** `Correctas ... Superado: 11` (7 de la vista del servidor + 4 del contrato con el cliente).
- **Reporta si falla:** el mensaje de error. Un error de *restore* suele ser falta de internet o del SDK.

---

## Bloque C — Contra UGS (el módulo de Cloud Code)

> Necesitas tu proyecto de UGS vinculado y la sesión iniciada en el editor.

### P6 · Desplegar el módulo
- **Pasos:** 1) *Window → Deployment*, 2) busca `ReactionDominoModule.ccmr` y marca el módulo, 3) *Deploy*, 4) abre el Dashboard de UGS → *Cloud Code → Modules*.
- **Resultado esperado:** el despliegue termina en verde y el módulo `ReactionDominoModule` muestra 4 funciones: `GetGameConfig`, `CreateMatch`, `PlayMove`, `GetMatchState`.
- **Reporta si falla:** el mensaje del panel de Deployment completo. Los fallos típicos son falta del SDK de .NET 8, o que el nombre del módulo no coincida.

### P7 · Dos jugadores y un código de partida
Necesitas **dos sesiones distintas**: el editor con tu cuenta de invitado y otra instancia (otro editor o un build) con una cuenta de usuario/contraseña.

Para llamar al módulo usa el **arnés de pruebas** `OnlineDebugHarness` (`Assets/scrips/Online`). No es el juego: es una herramienta con acciones numeradas que imprimen en la consola lo que responde el servidor.
1. Abre `SampleScene`, crea un GameObject vacío y añádele el componente **Online Debug Harness**.
2. Dale Play e inicia sesión con la interfaz normal (invitado, o usuario y contraseña en la otra instancia).
3. En el Inspector, clic derecho sobre el nombre del componente: aparecen `1 - Suscribirme al push`, `2 - GetGameConfig`, `3 - CreateMatch`, `4 - PlayMove`, `5 - GetMatchState` y `6 - Prueba de trampa`. El código de partida (`matchCode`) y la jugada (`x`, `y`) se editan en el Inspector.

- **Pasos:** 1) sesión A: acción `2 - GetGameConfig`, 2) A: `3 - CreateMatch` con el código `PRUEBA1`, 3) B: `3 - CreateMatch` con el **mismo** código, 4) los dos: `5 - GetMatchState`.
- **Resultado esperado:** `GetGameConfig` imprime `protocolo=1 tablero=6 nivelMax=4 turnosMax=30`. Tras el paso 2: `fase=waiting tuAsiento=1`. Tras el paso 3: `fase=playing tuAsiento=2` para B y `turnoDe=1`; el tablero se imprime como cuadrícula (`R` = rosado, `N` = naranja, el número es el nivel).
- **Reporta si falla:** la respuesta o el error completo. Un error de "módulo no encontrado" indica P6 incompleta.

### P8 · Idempotencia de `CreateMatch`
- **Pasos:** A llama de nuevo a `CreateMatch("PRUEBA1")`. Después una tercera cuenta (o cambia de invitado a otra) llama con el mismo código.
- **Resultado esperado:** A recibe **la misma partida sin cambios** (sigue siendo el asiento 1; no se crea otra). La tercera cuenta recibe un **error de partida completa**.
- **Reporta:** qué devolvió cada llamada.

### P9 · El servidor rechaza las trampas
Con la partida `rd-PRUEBA1` en juego (turno del asiento 1 = cuenta A):
- **Pasos y resultados esperados:**
  1. B hace `PlayMove(2,2)` (fuera de turno) → `Outcome: "not_your_turn"` y el estado **no cambia** (`TotalMoves` igual).
  2. A hace `PlayMove(9,9)` → `Outcome: "out_of_range"`.
  3. A hace `PlayMove(2,2)` → `Outcome: "ok"`. Luego B hace `PlayMove(2,2)` → `Outcome: "cell_owned_by_rival"`.
  4. Una cuenta que **no** es de la partida llama a `PlayMove` o a `GetMatchState` → **error** "No eres jugador de esta partida".
- **Reporta si falla:** la respuesta exacta. Si una jugada ilegal **se acepta**, es un fallo grave: dime qué mandaste.
- **Para aprender:** el `Outcome` de un rechazo **no** es una excepción. ¿Por qué crees que se diseñó así?

### P10 · El push llega al rival
- **Qué probar:** que el servidor avisa al rival sin mandarle el estado.
- **Pasos:** 1) en la sesión B, antes de jugar, ejecuta `1 - Suscribirme al push`, 2) A juega una jugada válida (`4 - PlayMove`), 3) mira la consola de B.
- **Resultado esperado:** B imprime `AVISO recibido para la partida 'rd-PRUEBA1' -> pido el estado al servidor.` y, justo después, el estado actualizado (lo pide el propio B con `GetMatchState`: el aviso **no trae datos**). Lo mismo ocurre en A cuando B se une a la partida. Si no llega ningún push, el sondeo de respaldo debería avisar en ~10 s (consola: mismo mensaje): reporta cuál de los dos pasó.
- **Reporta si falla:** si no llega nada, si llega con más datos, o si no compila la suscripción. Que el push falle **no** debe hacer fallar la jugada: comprueba que `PlayMove` responde `ok` igual.

### P11 · Terminar una partida y que el premio se pague una sola vez
- **Pasos:** juega una partida completa entre A y B (puedes forzar una explosión en cadena como la de las pruebas del Core: A juega 4 veces en una celda y B 3 veces en una celda vecina). Luego: 1) llama varias veces a `GetMatchState`, 2) intenta otra `PlayMove`, 3) en el Dashboard abre *Cloud Save → Player data* de cada cuenta y busca `onlineProfile`.
- **Resultado esperado:** `Phase: "finished"`, `WinnerSeat` correcto, `AwardedCoins/Gems` en la vista (100/50 al ganador, 25/10 al perdedor). La jugada extra devuelve `match_finished`. `onlineProfile` aparece en la sección **Protected** de **las dos cuentas**, y **no cambia** por más `GetMatchState` que llames (`Matches` +1 una sola vez).
- **Reporta si falla:** los valores que viste. Premios duplicados o ausentes en un jugador son un fallo grave.

### P12 · El cliente no puede falsificar el saldo (clases de acceso)
- **Qué probar:** que escribir desde el cliente **no** altera lo que decide el servidor.
- **Pasos:** 1) ejecuta `6 - Prueba de trampa` (guarda `onlineProfile = "trampa"` desde el cliente con `Data.Player.SaveAsync`), 2) mira el Dashboard de esa cuenta en Cloud Save, 3) ejecuta `5 - GetMatchState` de una partida terminada.
- **Resultado esperado:** la escritura del cliente crea un item en **Default** (otro dato distinto); el item **Protected** `onlineProfile` queda **intacto**.
- **Reporta:** qué viste en cada sección. **Explica con tus palabras** por qué el servidor guarda ahí y no en Default (ver el comentario de `PlayerRewardStore.cs`), y qué pasa hoy con `Coins`/`Gems` de `UGSProfileManager`: sigue habiendo **un hueco** que debes cerrar en la Tarea 3.

---

## Qué NO está probado (y por tanto es sospechoso)

- Que el **juego funcione jugando** (P2): compila y las pruebas del Core pasan, pero nadie ha dado Play ni visto las animaciones de explosión con el cambio de `GridPoint`.
- El **arnés y el servicio de cliente** (`OnlineMatchService`, `OnlineDebugHarness`): compilan en Unity y sus DTOs se comprobaron contra el JSON del servidor con `dotnet test`, pero nunca se ejecutaron contra UGS (ni la suscripción al push ni `CallModuleEndpointAsync`).
- Que las llamadas a Cloud Save y al push funcionen contra UGS real: **compilan**, pero nunca se ejecutaron (P6–P11).
- Plazo por turno, abandono, reconexión con la partida a medias, y partidas simultáneas del mismo jugador: no existen todavía.
