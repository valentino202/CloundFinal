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

### Paso 0 — Verificar lo que ya sirve (sin código) ✅
- [ ] Auth funciona (invitado + usuario/contraseña, PlayerId único).
- [ ] Cloud Save guarda pack/color/monedas por jugador.
- [ ] `PlayerPrefs` tiene `SelectedMatchMode` (0 normal / 1 infinito) y
      `SelectedMaxTurns` (30..100 de 10 en 10).
- [ ] Leaderboards normal e infinito funcionan.

### Paso 1 — Modelo de "busqueda" (solo datos, sin red) ⭐
- **Qué**: una clase `BusquedaPvP` con `{ modo, maxTurns, playerId }`.
  Es lo que un jugador "pone" en la cola.
- **Prueba**: imprimir por consola la búsqueda al dar Play.
- **Sin riesgo**: no cambia el juego.

### Paso 2 — Espejo por cliente (ya diseñado, se hace antes de red) ⭐⭐
- **Qué**: cada pantalla ve al jugador LOCAL "abajo" con su pack/color, y al
  rival "arriba". `SlotLocal(owner) = owner == viewerId ? 1 : 2`.
  `CellVisuals`/`PackFor` ya consultan por dueño (pasos 2-3 del plan viejo).
- **Prueba**: simular 2 jugadores locales (viewerId=1 y 2) en la misma máquina.
- **Por qué ahora**: es visual y no depende de la red.

### Paso 3 — Simular 2 jugadores en la misma máquina ⭐⭐
- **Qué**: 2 instancias del `GameManager` (o 2 ventanas) con un `MatchController`
  cada una, turnos alternados `humanId` 1 y 2, sin internet todavía.
- **Prueba**: jugar una partida completa pasando el turno de "jugador 1" a
  "jugador 2" en la misma pantalla.
- **Objetivo**: validar que la lógica (`GameLogic.PlacePoint`) es
  indiferente a quién mueve.

### Paso 4 — Lobby: crear / unirse con filtros ⭐⭐⭐
- **Qué**: servicio de Lobby (UGS Lobby). El jugador crea o se une a una sala
  con atributos `{ mode, maxTurns }`.
- **Filtro**: `maxTurns == 40` solo empareja con otro de 40. Infinito filtra
  solo por `mode=1`.
- **Prueba**: 2 sesiones (2 cuentas / 2 builds) se encuentran en la misma sala.

### Paso 5 — Sincronizar el estado de la partida ⭐⭐⭐⭐
- **Qué**: enviar por red solo la **jugada** `(x, y, playerId)`, no el tablero
  entero. Cada cliente aplica `GameLogic.PlacePoint` en su copia y obtiene el
  mismo resultado (determinista).
- **Autoridad**: un cliente (host) valida el turno y difunde el resultado.
- **Prueba**: mover en un cliente y ver el tablero actualizado en el otro.

### Paso 6 — Sincronizar turnos y fin de partida ⭐⭐⭐⭐
- **Qué**: quién mueve, contador de turnos (`maxTurns` o ∞), y el evento de
  victoria/empate. Evitar que ambos muevan a la vez (`busy` por red).
- **Prueba**: partida infinita completa entre 2 clientes.

### Paso 7 — Normal con límite de turnos (30–100) ⭐⭐⭐⭐
- **Qué**: el `maxTurns` elegido viaja en el Lobby y se aplica en ambos
  clientes. Al llegar al límite gana quien tiene más casillas
  (`GameLogic.WinnerByCells`, ya existe).
- **Prueba**: elegir 40 en ambos → partida se corta en el turno 40.

### Paso 8 — Recompensas y perfil en PvP ⭐⭐⭐⭐⭐
- **Qué**: el ganador reporta al Cloud Code `ValidateMatchResult` (ya existe)
  con `winnerId`/`loserId` reales (en vez de "BOT"). Monedas/gemas y ranking
  se guardan por cuenta.
- **Prueba**: ganar en la cuenta A → A sube de monedas y ranking; B no.

### Paso 9 — Robustez ⭐⭐⭐⭐⭐
- [ ] Reconectar si se cae la red.
- [ ] Abandonar partida / rendición.
- [ ] Timeout de matchmaking (si no aparece nadie con 40, avisar).
- [ ] Anti-trampas: el host valida que la jugada sea legal.

---

## REGLA DE ORO (lección 17)
**Un paso a la vez → probar → commit.** El PvP toca `GameManager` y
`MatchController` (el corazón del juego). Cualquier paso que rompa el juego
local se revierte al commit anterior. El juego local debe seguir funcionando
**siempre** como modo de prueba (bot) aunque el online falle.

---

## ESTADO ACTUAL
⏸ **No se escribió código de PvP.** Este MD es solo el plan.
El juego sigue 100% local (humano vs bot) y listo para entregar.
