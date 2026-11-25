# PVP ONLINE — Plan ordenado (fácil → difícil)

> Plan para convertir el juego (hoy 100% local: humano vs bot) en **2 jugadores
> conectados por internet**. NO se escribe código hasta elegir el servicio.
> Los datos de modo/turnos ya existen en `PlayerPrefs` (`SelectedMatchMode`,
> `SelectedMaxTurns`) y los lee `GameManager.Start` (líneas 61-74).
>
> Modos según el usuario:
> - **Infinito**: 2 jugadores dan Play → se emparejan → sin límite de turnos.
> - **Normal (30–100)**: el jugador elige p. ej. 40 → el sistema busca OTRO
>   que esté buscando exactamente 40 (filtro por atributo `maxTurns`).

---

## ⚠️ DECISIÓN PREVIA (antes del paso 1)

Elegir el servicio de red. Recomendación: **Unity Netcode + Lobby** (todo
dentro de UGS, ya usás Auth/Cloud Save/Leaderboards, sin cuenta externa).
Alternativa: Photon (más robusto, pero servicio externo).

---

## PASOS (fácil → difícil)

### Paso 0 — Verificar lo que ya sirve (sin código) ✅ (verificado por código)
- [x] Auth: anónimo/invitado + usuario/contraseña (alta, login, link y
      logout). PlayerId único por sesión (`UGSAuthManager`).
- [x] Cloud Save: claves `OwnedPacks`, `EquippedPack`, `Coins`, `Gems` (+J2)
      guardadas y leídas por cuenta, con respaldo local por PlayerId
      (`UGSProfileManager`).
- [x] `PlayerPrefs` tiene `SelectedMatchMode` (0/1) y `SelectedMaxTurns`
      (30..100): UIManager los ESCRIBE con clamp y GameManager los LEE
      (líneas 65-68) — misma clave, mismo rango.
- [x] Leaderboards: `RankingView` lee `ranked_normal` / `ranked_infinito`
      (asignados en escena) y `ValidateMatchResult.js` ESCRIBE el correcto
      según `matchMode` (0→normal, 1→infinito) al GANAR.
- [ ] **Prueba manual en Play** (no se puede verificar por código):
      1. Login invitado + usuario/contraseña → cada uno sus datos.
      2. Comprar caldero con una cuenta → otra cuenta NO lo tiene.
      3. Elegir Normal 40 → reiniciar → sigue en 40.
      4. Ganar → ranking sube (requiere Cloud Code desplegado en UGS).

### Paso 1 — Modelo de "busqueda" (solo datos, sin red) ⭐ ✅ HECHO
- **Qué**: una clase `BusquedaPvP` con `{ modo, maxTurns, playerId }`.
  Es lo que un jugador "pone" en la cola.
- **Implementado**: `Assets/scrips/pvp/BusquedaPvP.cs` (commit del Paso 1).
  - `BusquedaPvP.Crear(mode, maxTurns)` toma la selección del menú.
  - `CompatibleCon(otra)` = regla de emparejamiento (Normal exige maxTurns
    EXACTO; Infinito solo pide mismo modo). Se usará en el Paso 4.
  - `ToString()` legible para consola.
- **Conectado en** `UIManager.SelectModeAndPlay`: al dar Play imprime
  `[PvP] Buscando partida... modo=Normal (40 turnos) playerId=...`
- **Prueba**: dar Play y ver el log en consola.
- **Sin riesgo**: no cambia el juego (solo un Debug.Log).

### Paso 2 — Espejo por cliente ❌ DESCARTADO por el usuario
- **Qué era**: cada pantalla ve al jugador LOCAL "abajo" con su pack/color, y al
  rival "arriba". `SlotLocal(owner) = owner == viewerId ? 1 : 2`.
  `CellVisuals`/`PackFor` ya consultan por dueño (pasos 2-3 del plan viejo).
- **Decisión**: NO se hace (el usuario: "el 2 no"). Si en el futuro hace falta,
  retomar de acá.

### Paso 3 — Simular 2 jugadores en la misma máquina ⭐⭐ ✅ IMPLEMENTADO (prueba)
- **Qué**: clase pura `GameLogic` ya es indiferente al jugador (PlacePoint recibe
  `player` 1/2 y valida `player == currentPlayer`). El bot solo es un decisor
  de celdas. Este paso solo CONFIRMA eso con una prueba.
- **Implementado**: `Assets/scrips/pvp/Paso3PruebaDosJugadores.cs` (MonoBehaviour).
  - Crea un GameLogic (6x6, nivel 4, 30 turnos) igual que el juego real.
  - Juega una partida completa alternando jugador 1 -> 2 -> 1...
  - Modo configurable: BotAgent para AMBOS (mas realista) o celda legal al azar.
  - Imprime cada jugada y el resultado final (ganador, casillas, turnos).
  - Tope de seguridad (200 movimientos) para no colgarse en infinito.
- **Prueba**: adjuntar a cualquier GameObject, dar Play, mirar la consola.
  Si termina con "[Paso3] FIN DE PARTIDA" sin errores rojos -> la logica es
  indiferente al jugador.
- **Sin riesgo**: archivo nuevo, no toca GameManager/MatchController/escena.

### Paso 4 — Lobby: crear / unirse con filtros ⭐⭐⭐ 🟡 ESQUELETO
- **Qué**: servicio de Lobby (UGS Lobby). El jugador crea o se une a una sala
  con atributos `{ mode, maxTurns }`.
- **Filtro**: `maxTurns == 40` solo empareja con otro de 40. Infinito filtra
  solo por `mode=1`.
- **Prueba**: 2 sesiones (2 cuentas / 2 builds) se encuentran en la misma sala.
- **Esqueleto**: `Assets/scrips/pvp/PvpLobby.cs` (CrearSala, BuscarSalas,
  UnirseSala, SalirSala). TODOs con la API real de UGS Lobby.
- **Paquete faltante**: `com.unity.services.lobby` (instalar con el profesor).

### Paso 5 — Sincronizar el estado de la partida ⭐⭐⭐⭐ 🟡 ESQUELETO
- **Qué**: enviar por red solo la **jugada** `(x, y, playerId)`, no el tablero
  entero. Cada cliente aplica `GameLogic.PlacePoint` en su copia y obtiene el
  mismo resultado (determinista).
- **Autoridad**: un cliente (host) valida el turno y difunde el resultado.
- **Prueba**: mover en un cliente y ver el tablero actualizado en el otro.
- **Esqueleto**: `Assets/scrips/pvp/PvpSync.cs` (EnviarJugada, AplicarJugadaRemota,
  RecibirJugadaDeRed). Reutiliza GameLogic (validado en Paso 3).
- **Paquete faltante**: `com.unity.netcode.gameobjects`.

### Paso 6 — Sincronizar turnos y fin de partida ⭐⭐⭐⭐ 🟡 ESQUELETO
- **Qué**: quién mueve, contador de turnos (`maxTurns` o ∞), y el evento de
  victoria/empate. Evitar que ambos muevan a la vez (`busy` por red).
- **Prueba**: partida infinita completa entre 2 clientes.
- **Esqueleto**: `Assets/scrips/pvp/PvpTurnos.cs` (JugadorActual, MaxTurns,
  IntentarJugar, ConfirmarJugada, eventos OnTurnoCambiado/OnFinPartida).

### Paso 7 — Normal con límite de turnos (30–100) ⭐⭐⭐⭐
- **Qué**: el `maxTurns` elegido viaja en el Lobby y se aplica en ambos
  clientes. Al llegar al límite gana quien tiene más casillas
  (`GameLogic.WinnerByCells`, ya existe).
- **Prueba**: elegir 40 en ambos → partida se corta en el turno 40.
- **Nota**: ya funciona en GameLogic (maxTurns + WinnerByCells). Solo falta
  que el valor viaje por red (Paso 4/5).

### Paso 8 — Recompensas y perfil en PvP ⭐⭐⭐⭐⭐ 🟡 ESQUELETO
- **Qué**: el ganador reporta al Cloud Code `ValidateMatchResult` (ya existe)
  con `winnerId`/`loserId` reales (en vez de "BOT"). Monedas/gemas y ranking
  se guardan por cuenta.
- **Prueba**: ganar en la cuenta A → A sube de monedas y ranking; B no.
- **Esqueleto**: `Assets/scrips/pvp/PvpRecompensas.cs`. Este SI puede llamar
  al endpoint real (UGSCloudCodeManager ya esta cableado); solo cambia que
  los ids son reales.

### Paso 9 — Robustez ⭐⭐⭐⭐⭐ 🟡 ESQUELETO
- [ ] Reconectar si se cae la red.
- [ ] Abandonar partida / rendición.
- [ ] Timeout de matchmaking (si no aparece nadie con 40, avisar).
- [ ] Anti-trampas: el host valida que la jugada sea legal.
- **Esqueleto**: `Assets/scrips/pvp/PvpRobustez.cs` (Reconectar, Abandonar,
  TimeoutMatchmaking, ValidarJugadaLegal que reutiliza GameLogic.IsValidMove).

---

## ESQUELETOS CREADOS (no implementados, sin riesgo)

Todos en `Assets/scrips/pvp/`. Son clases sueltas que **no tocan el juego
real** (no se instancian en ninguna escena, no se llaman desde GameManager).
Compilan solos. Se ensamblan con el profesor.

| Archivo | Paso | Qué hace (cuando se implemente) |
|---|---|---|
| `PvpEstado.cs` | — | Enum de estados (Buscando/EnSala/Jugando/...) |
| `PvpLobby.cs` | 4 | Crear/buscar/unirse/salir de salas UGS Lobby |
| `PvpSync.cs` | 5 | Enviar/recibir jugadas (x,y,playerId) |
| `PvpTurnos.cs` | 6 | Turno actual, maxTurns, busy, fin de partida |
| `PvpRecompensas.cs` | 8 | Reporta resultado a Cloud Code (este SI llama al endpoint real) |
| `PvpRobustez.cs` | 9 | Reconectar, abandonar, timeout, anti-trampas |
| `PvpManager.cs` | — | Coordinador: IniciarBusqueda, IniciarPartida, FinalizarPartida |

Paquetes a instalar con el profesor:
- `com.unity.services.lobby` (Paso 4)
- `com.unity.netcode.gameobjects` (Paso 5+)

Los TODOs dentro de cada archivo muestran la llamada exacta a la API.

---

## REGLA DE ORO (lección 17)
**Un paso a la vez → probar → commit.** El PvP toca `GameManager` y
`MatchController` (el corazón del juego). Cualquier paso que rompa el juego
local se revierte al commit anterior. El juego local debe seguir funcionando
**siempre** como modo de prueba (bot) aunque el online falle.

---

## ESTADO ACTUAL
▶ **Paso 0 casi completo**: verificado TODO por código. Falta prueba manual en Play.
▶ **Paso 1 completo**: modelo `BusquedaPvP` + log al dar Play.
❌ **Paso 2 descartado** por el usuario.
▶ **Paso 3 validado**: prueba de lógica 2 jugadores sin errores.
🟡 **Pasos 4-9: ESQUELETOS creados** (no implementados, sin riesgo, compilan solos):
   PvpLobby, PvpSync, PvpTurnos, PvpRecompensas, PvpRobustez, PvpManager, PvpEstado.
   Listos para ensamblar con el profesor (faltan paquetes Lobby/Netcode).
Sigue sin escribirse red: el juego sigue 100% local (humano vs bot) y
listo para entregar.
Próximo (con profesor): elegir servicio (Netcode+Lobby vs Photon) e instalar
paquetes, luego implementar los TODOs de los esqueletos.
