# Actividades de testing

Estas pruebas son **tuyas**: el profesor y Claude Code hicieron los cambios, pero **nadie los ha ejecutado dentro de Unity ni contra UGS**. Lo único verificado hasta ahora es que el Core y el módulo de Cloud Code **compilan y pasan sus pruebas con `dotnet`** (37 pruebas del Core y 7 del módulo). Tu retroalimentación es la que cierra el ciclo.

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
- **Resultado esperado:** `Correctas ... Superado: 7`.
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

Para llamar al módulo todavía no existe el cliente (es la Tarea 5), así que usa un script **temporal** (no lo subas al repo). Es solo una guía; los nombres de la API pueden variar con la versión del paquete (`2.10.4`) y, si algo no compila, mira la documentación de Cloud Code y repórtalo:

```csharp
// Script temporal para probar: adjúntalo a un GameObject, ya con la sesión de UGS iniciada.
async void Call(string fn, Dictionary<string, object> args)
{
    var res = await Unity.Services.CloudCode.CloudCodeService.Instance
        .CallModuleEndpointAsync<object>("ReactionDominoModule", fn, args);
    Debug.Log($"{fn} -> {res}");
}
// Ejemplos:
// Call("GetGameConfig", new Dictionary<string, object>());
// Call("CreateMatch",  new Dictionary<string, object> { { "matchCode", "PRUEBA1" } });
// Call("PlayMove",     new Dictionary<string, object> { { "matchId", "rd-PRUEBA1" }, { "x", 2 }, { "y", 2 } });
// Call("GetMatchState", new Dictionary<string, object> { { "matchId", "rd-PRUEBA1" } });
```

- **Pasos:** 1) sesión A: `GetGameConfig`, 2) A: `CreateMatch` con el código `PRUEBA1`, 3) B: `CreateMatch` con el **mismo** código, 4) los dos: `GetMatchState`.
- **Resultado esperado:** `GetGameConfig` devuelve `ProtocolVersion: 1` y reglas 6 / 4 / 30. Tras el paso 2: `Phase: "waiting"`, `MySeat: 1`. Tras el paso 3: `Phase: "playing"`, `MySeat: 2` para B y `CurrentSeat: 1`.
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
- **Pasos:** 1) en la sesión B, antes de jugar, suscríbete a los mensajes (ver abajo), 2) A juega una jugada válida, 3) mira la consola de B.
  ```csharp
  // Script temporal (la API puede variar con la versión del paquete):
  var callbacks = new Unity.Services.CloudCode.Subscriptions.SubscriptionEventCallbacks();
  callbacks.MessageReceived += m => Debug.Log($"PUSH tipo={m.MessageType} cuerpo={m.Message}");
  await Unity.Services.CloudCode.CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
  ```
- **Resultado esperado:** B recibe un mensaje de tipo `match_updated` con un cuerpo de la forma `{"type":"match_updated","matchId":"rd-PRUEBA1"}` y **nada más** (sin tablero ni la jugada). Lo mismo ocurre en A cuando B se une a la partida.
- **Reporta si falla:** si no llega nada, si llega con más datos, o si no compila la suscripción. Que el push falle **no** debe hacer fallar la jugada: comprueba que `PlayMove` responde `ok` igual.

### P11 · Terminar una partida y que el premio se pague una sola vez
- **Pasos:** juega una partida completa entre A y B (puedes forzar una explosión en cadena como la de las pruebas del Core: A juega 4 veces en una celda y B 3 veces en una celda vecina). Luego: 1) llama varias veces a `GetMatchState`, 2) intenta otra `PlayMove`, 3) en el Dashboard abre *Cloud Save → Player data* de cada cuenta y busca `onlineProfile`.
- **Resultado esperado:** `Phase: "finished"`, `WinnerSeat` correcto, `AwardedCoins/Gems` en la vista (100/50 al ganador, 25/10 al perdedor). La jugada extra devuelve `match_finished`. `onlineProfile` aparece en la sección **Protected** de **las dos cuentas**, y **no cambia** por más `GetMatchState` que llames (`Matches` +1 una sola vez).
- **Reporta si falla:** los valores que viste. Premios duplicados o ausentes en un jugador son un fallo grave.

### P12 · El cliente no puede falsificar el saldo (clases de acceso)
- **Qué probar:** que escribir desde el cliente **no** altera lo que decide el servidor.
- **Pasos:** 1) desde el cliente, con un script temporal, guarda `onlineProfile` con `CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>{{"onlineProfile","trampa"}})`, 2) mira el Dashboard de esa cuenta, 3) llama a `GetMatchState` de una partida terminada.
- **Resultado esperado:** la escritura del cliente crea un item en **Default** (otro dato distinto); el item **Protected** `onlineProfile` queda **intacto**.
- **Reporta:** qué viste en cada sección. **Explica con tus palabras** por qué el servidor guarda ahí y no en Default (ver el comentario de `PlayerRewardStore.cs`), y qué pasa hoy con `Coins`/`Gems` de `UGSProfileManager`: sigue habiendo **un hueco** que debes cerrar en la Tarea 3.

---

## Qué NO está probado (y por tanto es sospechoso)

- Que Unity compile los cambios del PR #2 (P1–P2): solo se comprobó el Core y el módulo con `dotnet`, no el proyecto completo.
- Que las llamadas a Cloud Save y al push funcionen contra UGS real: **compilan**, pero nunca se ejecutaron (P6–P11).
- Plazo por turno, abandono, reconexión con la partida a medias, y partidas simultáneas del mismo jugador: no existen todavía.
