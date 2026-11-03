# Protocolo de mensajes del modo online (plantilla para rellenar)

> Rellénalo **antes** de programar la Tarea 4. Borra las instrucciones en cursiva a medida que lo completes.
>
> **"El sistema de mensajes"** son los **mensajes push de Cloud Code**. Hay dos direcciones: el cliente le **pide** cosas al servidor (llamadas), y el servidor **avisa** al rival (push). Piensa los dos lados.

## 1. Llamadas cliente → servidor (funciones de Cloud Code)

*Una función por fila. El cliente envía intenciones, nunca "quién soy" ni "quién ganó": el servidor te identifica por tu token.*

| Función | Parámetros | Qué hace el servidor | Respuesta |
|---|---|---|---|
| `GetGameConfig` *(recomendada)* | | | |
| `CreateMatch` | | | |
| `PlayMove` | | | |
| `GetMatchState` | | | |

*Preguntas para cada una:*
- *`CreateMatch`: ¿qué pasa si se llama dos veces con lo mismo (reintento, o los dos jugadores)? Debe devolver la **misma** partida. ¿Cómo empareja a los jugadores: Matchmaker o un código de partida? ¿Quién es el jugador 1 y quién el 2, y cómo se decide sin que el cliente elija?*
- *`PlayMove`: ¿en qué orden valida? (¿existe la partida? ¿ya terminó? ¿eres jugador de ella? ¿te toca? ¿la jugada es legal según el motor?). ¿Qué pasa si se repite sobre una partida terminada?*
- *`GetMatchState`: además de devolver el estado, ¿qué otra cosa resuelve? (plazos vencidos, abandono).*

## 2. Mensaje servidor → cliente (push)

*El push es la parte "mensajes" del curso. Rellena:*

| Campo | Valor |
|---|---|
| Tipo de mensaje (`type`) | |
| Quién lo envía y cuándo | |
| A quién se envía | |
| Payload (¿qué lleva? **¿Por qué no lleva el estado ni la jugada?**) | |
| Qué hace el cliente al recibirlo | |
| Respaldo si el push se pierde (¿cada cuántos segundos?) | |
| ¿Qué pasa si el push falla al enviarse? ¿La jugada se pierde? | |

## 3. Estado de la partida

*¿Cómo se guarda? La propuesta del curso: una **lista de jugadas, solo de agregar**, con clave `matchId`, no una foto del tablero. En cada llamada el servidor la **reproduce** con un `GameLogic` nuevo. ¿Qué datos lleva cada jugada? ¿Qué más hay en el registro (jugadores, hora de la última actividad, inicio del turno, fase)? ¿Por qué esto exige reglas deterministas?*

*¿Qué recibe el cliente? Una **vista** (distinta del registro interno): tablero (`owner` y `level`), turno actual, número de jugadas, fase, tu asiento, ganador, explosiones de la última jugada para animar.*

## 4. Rechazos de una jugada

*Rechazos normales, no excepciones: por ejemplo `not_your_turn`, `invalid_cell`, `cell_owned_by_rival`, `match_finished`, `not_a_player`. ¿Cuál mostraría la pantalla y cómo?*

## 5. Diagrama de secuencia de un turno (con push)

*Dibuja (a mano o con texto): Jugador 1 `PlayMove` → servidor valida y guarda → push a Jugador 2 → Jugador 2 `GetMatchState` → pinta. Incluye el caso "el push no llega" (sondeo de respaldo) y el caso "Jugador 1 no recibe la respuesta de `PlayMove`".*

## 6. Datos de Cloud Save y su clase de acceso

*Cada dato que decide el servidor debe ir en Protected o Private, nunca en Default (el jugador puede escribir en Default con su token). Rellena:*

| Dato | Quién lo escribe | Clase de acceso (Default / Protected / Private) | ¿Por qué? |
|---|---|---|---|
| Monedas y gemas | | | |
| Victorias y partidas | | | |
| MMR / ranking | | | |
| Registro de la partida | | | |
| Color y pack equipado | | | |

## 7. Plazos, abandono y premios

1. ¿Cuánto dura un turno? (piensa en push + sondeo de ~10 s: ¿es viable 2 s? ¿y 20–30 s?)
2. ¿Quién mide el tiempo? (**el servidor**). ¿Cuándo se entera de que venció? (al consultar el estado).
3. ¿Cuánto sin actividad del rival se considera abandono y qué pasa con la partida?
4. ¿Quién da los premios y cuándo? ¿Cómo evitas que se paguen dos veces?

## 8. Versión del protocolo

*¿Dónde vive el número de versión? ¿Cuándo se sube? ¿Qué debería pasar si el cliente y el servidor no coinciden?*

## 9. Decisiones que debes justificar

1. ¿Quién es la autoridad (servidor o anfitrión) y por qué?
2. ¿Dónde viven las reglas para que cliente y servidor ejecuten **el mismo código**?
3. ¿Matchmaker o código de partida para el primer hito?
4. ¿Qué configuración fija el servidor al crear la partida (tamaño, nivel de explosión, tope de turnos, tiempo de turno)?
